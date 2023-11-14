using Microsoft.OpenApi.Models;

namespace API.Services;

public interface ISwaggerService
{
    Task<string> GetSwaggerJsonAsync(string apiName);
    Task<OpenApiDocument?> GetOpenApiDocumentAsync(string apiName);
}