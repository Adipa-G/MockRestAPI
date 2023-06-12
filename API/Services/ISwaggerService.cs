using Microsoft.OpenApi.Models;

namespace API.Services;

public interface ISwaggerService
{
    Task<string> GetSwaggerJsonAsync(string baseUrl, string apiName);
    Task<OpenApiDocument?> GetOpenApiDocumentAsync(string baseUrl, string apiName);
}