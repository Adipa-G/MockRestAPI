namespace API.Services;

public interface IResponseGeneratorService
{
    Task<KeyValuePair<string, string?>> GenerateJsonResponseAsync(string baseUrl, string apiName, string requestPath, HttpRequest request);
}