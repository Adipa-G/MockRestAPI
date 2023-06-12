namespace API.Services;

public interface ISwaggerExampleResponseBuilderService
{
    Task<string?> GetResponse(string baseUrl,string apiName, string requestPath, HttpRequest request);
}