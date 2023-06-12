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
    public class SwaggerService : ISwaggerService
    {
        private readonly ILogger<SwaggerService> _logger;
        private readonly EndpointOptions _endpointOptions;
        private readonly IMemoryCache _memoryCache;
        private readonly IFileSystem _fileSystem;
        private readonly IHttpClientFactory _httpClientFactory;

        public SwaggerService(ILogger<SwaggerService> logger,
            IOptions<EndpointOptions> endpointOptions,
            IMemoryCache memoryCache,
            IFileSystem fileSystem,
            IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _endpointOptions = endpointOptions.Value;
            _memoryCache = memoryCache;
            _fileSystem = fileSystem;
            _httpClientFactory = httpClientFactory;
        }

        public async Task<string> GetSwaggerJsonAsync(string baseUrl, string apiName)
        {
            var doc = await GetOpenApiDocumentAsync(baseUrl, apiName);
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

        public async Task<OpenApiDocument?> GetOpenApiDocumentAsync(string baseUrl, string apiName)
        {
            var cacheKey = $"open-api-document-{apiName}";
            if (_memoryCache.TryGetValue(cacheKey, out OpenApiDocument? doc))
            {
                return doc;
            }

            var apiDef = _endpointOptions.Apis.FirstOrDefault(api => api.ApiName == apiName);
            if (apiDef == null)
            {
                _logger.LogError("Could not find the API definition for API : [{apiName}] in the appsettings.json file", apiName);
                return null;
            }

            await using var stream = apiDef.SwaggerLocation.StartsWith("http")
                ? await OpenOpenApiDefinitionFromHttp(apiName, apiDef)
                : await OpenOpenApiDefinitionFromFile(apiName, apiDef);
            if (stream != null)
            {
                OpenApiDiagnostic? diagnostic = null;
                try
                {
                    doc = new OpenApiStreamReader().Read(stream, out diagnostic);
                    doc.Servers = new List<OpenApiServer>() { new() { Url = $"{baseUrl}/{apiName}" } };
                    _memoryCache.Set(cacheKey, doc, TimeSpan.FromSeconds(60));
                }
                catch (Exception e)
                {
                    var diagnosticJson = JsonConvert.SerializeObject(diagnostic);
                    _logger.LogError(e, "Could not parse the open api spec. Diagnostic details : [{diagnostic}]",
                        diagnosticJson);
                }
            }

            return doc;
        }

        private async Task<Stream?> OpenOpenApiDefinitionFromHttp(string apiName, EndpointOptionsApi apiDef)
        {
            try
            {
                var httpClient = _httpClientFactory.CreateClient();
                var stream = await httpClient.GetStreamAsync(apiDef.SwaggerLocation);
                return stream;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error trying to get the swagger file from URL: [{url}] for the api API: [{apiName}]", apiDef.SwaggerLocation, apiName);
                return null;
            }
        }

        private Task<Stream?> OpenOpenApiDefinitionFromFile(string apiName, EndpointOptionsApi apiDef)
        {
            try
            {
                var dir = GetBaseDirectory();
                if (dir == null)
                {
                    _logger.LogError(
                        "Could not find the Directory : [{baseDirectory}] in either the application directory or any of the parent directories",
                        _endpointOptions.RootFolderName);
                    return Task.FromResult((Stream?)null);
                }

                var swaggerPath = Path.Combine(dir.FullName, apiDef.SwaggerLocation);
                if (_fileSystem.File.Exists(swaggerPath))
                {
                    var stream = _fileSystem.File.Open(swaggerPath, FileMode.Open);
                    return Task.FromResult((Stream?)stream);
                }
                else
                {
                    _logger.LogError("Could not find the swagger file in Path : [{swaggerPath}] for the API : [{apiName}]", swaggerPath,
                        apiName);
                    return Task.FromResult((Stream?)null);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e,"Unknown error trying to open the swagger file for the API : [{apiName}]", apiName);
                return Task.FromResult((Stream?)null);
            }
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
