namespace API.Services;

public interface ISwaggerExampleResponseBuilderService
{
    Task<KeyValuePair<string, string?>> GetResponse(string apiName, string requestPath, HttpRequest request);
}