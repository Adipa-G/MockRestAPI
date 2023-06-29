using System.Collections.Concurrent;
using System.Dynamic;
using System.IO.Abstractions;
using System.Text.Json;

using API.Models;
using API.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;


namespace API.Services
{
    public class MockCallsLoader : IMockCallsLoader
    {
        private readonly ConfigOptions _configOptions;
        private readonly IFileSystem _fileSystem;
        private readonly IMemoryCache _memoryCache;
        private ILogger<MockCallsLoader> _logger;

        public MockCallsLoader(IOptions<ConfigOptions> configOptions,
            IFileSystem fileSystem,
            IMemoryCache memoryCache,
            ILogger<MockCallsLoader> mockLogger)
        {
            _configOptions = configOptions.Value;
            _fileSystem = fileSystem;
            _memoryCache = memoryCache;
            _logger = mockLogger;
        }

        public async Task LoadMockCallsAsync()
        {
            var rootFolderAbsolutePath = GetBaseDirectory();
            var allFiles = rootFolderAbsolutePath?.GetFiles("*.json", SearchOption.AllDirectories);

            if (allFiles == null || !allFiles.Any())
            {
                _logger.LogInformation("Did not found any mock calls in the folder {folder}", rootFolderAbsolutePath);
                return;
            }
                
            foreach (IFileInfo jsonFile in allFiles)
            {
                try
                {
                    var json = await _fileSystem.File.ReadAllTextAsync(jsonFile.FullName);
                    var apiList =  (IDictionary<string,object?>)JsonSerializer.Deserialize<ExpandoObject>(json)!;
                    foreach (var api in apiList)
                    {
                        var apiName = api.Key;
                        var apiValue = api.Value;
                        var pathList = (IDictionary<string, object?>)JsonSerializer.Deserialize<ExpandoObject>(apiValue?.ToString() ?? string.Empty)!;

                        foreach (var path in pathList)
                        {
                            var pathName = path.Key;
                            var pathValue = path.Value;
                            var methodList = (IDictionary<string, object?>)JsonSerializer.Deserialize<ExpandoObject>(pathValue?.ToString() ?? string.Empty)!;

                            ProcessMethod(methodList, apiName, pathName, jsonFile);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Unable to parse the file {0}", jsonFile);
                }
            }
        }

        private void ProcessMethod(IDictionary<string, object?> methodList, string apiName, string pathName, IFileInfo jsonFile)
        {
            foreach (var method in methodList)
            {
                var methodName = method.Key;
                var methodValue = method.Value;

                var cacheKey = $"{apiName.ToLower()}-{methodName.ToUpper()}-{pathName.ToLower().TrimStart('/')}";
                var calls = _memoryCache.GetOrCreate(cacheKey, _ => new List<MockApiCall>());

                var dtos = JsonSerializer.Deserialize<MockApiCall[]>(
                    methodValue?.ToString() ?? string.Empty,
                    new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                if (dtos != null)
                {
                    foreach (MockApiCall mockApiCall in dtos)
                    {
                        var idMappings =
                            _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
                        if (idMappings?.ContainsKey(mockApiCall.CallId!) ?? true)
                        {
                            _logger.LogError("Ignoring the call {0} from the file {1} as the CallId is already exists",
                                mockApiCall.CallId, jsonFile);
                            continue;
                        }

                        idMappings?.TryAdd(mockApiCall.CallId!, cacheKey);
                        mockApiCall.Expiry =
                            DateTimeOffset.Now.Add(
                                TimeSpan.FromSeconds(mockApiCall.TimeToLive.GetValueOrDefault(int.MaxValue)));
                        calls?.Add(mockApiCall);
                    }
                }

                var absoluteExpiration = (calls != null && calls.Any()) ? calls.Max(c => c.Expiry) : DateTimeOffset.Now;
                _memoryCache.Set(cacheKey, calls, absoluteExpiration);
            }
        }

        private IDirectoryInfo? GetBaseDirectory()
        {
            return DirectoryUtils.GetBaseDirectory(_fileSystem, _configOptions.MockApiCallsSubFolder);
        }
    }
}
