namespace API.Services;

public interface ISwaggerExampleResponseBuilderService
{
    Task<KeyValuePair<string, string?>> GetResponse(string baseUrl,string apiName, string requestPath, HttpRequest request);
}