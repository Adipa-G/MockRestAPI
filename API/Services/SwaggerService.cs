using System.IO.Abstractions;

using API.Options;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Readers;
using Microsoft.OpenApi.Writers;
using Newtonsoft.Json;

namespace API.Services
{
    public class SwaggerService
    {
        private readonly ILogger<SwaggerService> _logger;
        private readonly EndpointOptions _endpointOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly IFileSystem _fileSystem;

        public SwaggerService(ILogger<SwaggerService> logger,
            IOptions<EndpointOptions> endpointOptions,
            IMemoryCache memoryCache,
            IFileSystem fileSystem)
        {
            _logger = logger;
            _endpointOptions = endpointOptions.Value;
            _memoryCache = memoryCache;
            _fileSystem = fileSystem;
        }

        public async Task<string> GetSwaggerJson(string baseUrl, string apiName)
        {
            var doc = await GetOpenApiDocument(baseUrl, apiName);
            if (doc == null)
            {
                return string.Empty;
            }
            
            using var memoryStream = new MemoryStream();
            var openApiWriter = new OpenApiJsonWriter(new StreamWriter(memoryStream));
            doc.SerializeAsV3(openApiWriter);
            openApiWriter.Flush();

            memoryStream.Seek(0, SeekOrigin.Begin);
            var textReader = new StreamReader(memoryStream);
            var json = await textReader.ReadToEndAsync();
            return json;
        }

        public async Task<OpenApiDocument?> GetOpenApiDocument(string baseUrl, string apiName)
        {
            var cacheKey = $"open-api-document-{apiName}";
            if (_memoryCache.TryGetValue(cacheKey, out OpenApiDocument? doc))
            {
                return doc;
            }

            var apiDef = _endpointOptions.Apis.FirstOrDefault(api => api.ApiName == apiName);
            if (apiDef == null)
            {
                _logger.LogError("Could not find the API definition for {apiName} in the appsettings.json file", apiName);
                return null;
            }

            var dir = GetBaseDirectory();
            if (dir == null)
            {
                _logger.LogError("Could not find the directory {baseDirectory} in either the application directory or any of the parent directories", _endpointOptions.RootFolderName);
                return null;
            }

            var swaggerPath = Path.Combine(dir.FullName, apiDef.SwaggerLocation);
            if (_fileSystem.File.Exists(swaggerPath))
            {
                await using var fileStream = _fileSystem.File.Open(swaggerPath, FileMode.Open);
                OpenApiDiagnostic? diagnostic = null;
                try
                {
                    doc = new OpenApiStreamReader().Read(fileStream, out diagnostic);
                    doc.Servers = new List<OpenApiServer>()
                    {
                        new()
                        {
                            Url = $"{baseUrl}/{apiName}"
                        }
                    };
                    _memoryCache.Set(cacheKey, doc, TimeSpan.FromSeconds(60));
                }
                catch (Exception e)
                {
                    var diagnosticJson = JsonConvert.SerializeObject(diagnostic);
                    _logger.LogError(e,"Could not parse the open api spec. Diagnostic details : {diagnostic}", diagnosticJson);
                }
                return doc;
            }
            _logger.LogError("Could not find the swagger file in path {swaggerPath} for the api {apiName}", swaggerPath, apiName);
            return null;
        }

        private IDirectoryInfo? GetBaseDirectory()
        {
            var dir = _fileSystem.DirectoryInfo.New(AppDomain.CurrentDomain.BaseDirectory);
            do
            {
                if (dir.GetDirectories().Any(d => d.Name == _endpointOptions.RootFolderName))
                {
                    dir = dir.GetDirectories().FirstOrDefault(d => d.Name == _endpointOptions.RootFolderName);
                    break;
                }
                dir = dir.Parent;
            } while (dir?.Parent != null);
            return dir;
        }
    }
}
