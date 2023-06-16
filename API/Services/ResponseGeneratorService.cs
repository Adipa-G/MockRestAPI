using API.Models;

using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.Services
{
    public class ResponseGeneratorService : IResponseGeneratorService
    {
        private readonly ILogger<ResponseGeneratorService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ISwaggerExampleResponseBuilderService _swaggerExampleResponseBuilderService;

        public ResponseGeneratorService(ILogger<ResponseGeneratorService> logger,
            IMemoryCache memoryCache,
            ISwaggerExampleResponseBuilderService swaggerExampleResponseBuilderService)
        {
            _logger = logger;
            _memoryCache = memoryCache;
            _swaggerExampleResponseBuilderService = swaggerExampleResponseBuilderService;
        }

        public async Task<KeyValuePair<string, string?>> GenerateJsonResponseAsync(string baseUrl, string apiName, string requestPath, HttpRequest request)
        {
            var cacheKey = $"{apiName.ToLower()}-{request.Method.ToUpper()}-{requestPath.ToLower()}";
            var apiCalls = _memoryCache.Get<List<MockApiCall>?>(cacheKey);
            var match = await FindMatch(apiCalls, request);

            if (match != null)
            {
                return new KeyValuePair<string, string?>(match.ResponseCode.ToString(), JsonConvert.SerializeObject(match.Response));
            }
            
            var response =
                await _swaggerExampleResponseBuilderService.GetResponse(baseUrl, apiName, requestPath, request);
            if (!response.Equals(default(KeyValuePair<string, string?>)))
            {
                return response;
            }

            _logger.LogWarning("Unable to handle the path {path}", $"{apiName}/{requestPath}");
            var noIdeaMessage = new KeyValuePair<string, string?>("404", "I have never met this man in my life.");
            return noIdeaMessage;
        }

        private async Task<MockApiCall?> FindMatch(List<MockApiCall>? apiCalls, HttpRequest request)
        {
            if (apiCalls == null)
                return null;

            foreach (MockApiCall apiCall in apiCalls)
            {
                if (apiCall.Expiry < DateTimeOffset.Now)
                    continue;

                var match = CheckForQueryParameterMatch(request, apiCall);
                match = match && CheckForHeaderMatch(request, apiCall);
                match = match && await CheckBodyPathMatch(request, apiCall);
                if (match)
                {
                    return apiCall;
                }
            }
            return null;
        }

        private bool CheckForQueryParameterMatch(HttpRequest request, MockApiCall apiCall)
        {
            if (apiCall.QueryParamsToMatch == null)
                return true;

            if (request.Query.Count == 0)
                return false;

            var matchQueryGroupList = apiCall.QueryParamsToMatch.GroupBy(q => q.Key).Select(g =>
                new KeyValuePair<string, List<string>>(g.Key, g.Select(gv => gv.Value).ToList())).ToList();

            var queryGroupList = request.Query.Select(q =>
                new KeyValuePair<string, List<string?>>(q.Key, q.Value.ToList())).ToList();

            return MatchGroupList(matchQueryGroupList, queryGroupList);
        }

        private bool CheckForHeaderMatch(HttpRequest request, MockApiCall apiCall)
        {
            if (apiCall.HeadersToMatch == null )
                return true;

            if (request.Headers.Count == 0)
                return false;

            var matchQueryGroupList = apiCall.HeadersToMatch.GroupBy(q => q.Key).Select(g =>
                new KeyValuePair<string, List<string>>(g.Key, g.Select(gv => gv.Value).ToList())).ToList();

            var headerGroupList = request.Headers.Select(q =>
                new KeyValuePair<string, List<string?>>(q.Key, q.Value.ToList())).ToList();

            return MatchGroupList(matchQueryGroupList, headerGroupList);
        }

        private async Task<bool> CheckBodyPathMatch(HttpRequest request, MockApiCall apiCall)
        {
            if (apiCall.BodyPathsToMatch == null)
                return true;

            if (!request.HasJsonContentType() && request.ContentLength.GetValueOrDefault() == 0)
                return false;

            var requestObject = await request.ReadFromJsonAsync<JObject>();

            bool isMatch = true;
            foreach (var bodyPath in apiCall.BodyPathsToMatch)
            {
                var pathValue = requestObject?.SelectToken(bodyPath.Key);
                isMatch = isMatch && pathValue?.Value<string>() == bodyPath.Value;
            }
            return isMatch;
        }

        private bool MatchGroupList(List<KeyValuePair<string, List<string>>> matchGroupList, List<KeyValuePair<string, List<string?>>> queryGroupList)
        {
            var isMatch = true;
            foreach (var matchGroup in matchGroupList)
            {
                isMatch = isMatch && queryGroupList.Any(q =>
                    q.Key == matchGroup.Key && q.Value.Any(v => v != null && matchGroup.Value.Contains(v)));
                if (!isMatch)
                    return false;
            }

            return isMatch;
        }
    }
}
