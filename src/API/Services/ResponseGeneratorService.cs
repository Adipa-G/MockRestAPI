using System.Text.Json;
using System.Text.Json.Nodes;

using API.Models;

using Microsoft.Extensions.Caching.Memory;

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
                return new KeyValuePair<string, string?>(match.ResponseCode.ToString(), JsonSerializer.Serialize(match.Response));
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

            string requstJson;
            using (StreamReader stream = new(request.Body))
            {
                requstJson = await stream.ReadToEndAsync();
            }

            var highScoreCalls = FindMatchingCalls(apiCalls, request, requstJson);

            foreach (MockApiCall apiCall in highScoreCalls)
            {
                if (apiCall.ReturnOnlyForNthMatch.GetValueOrDefault() > 0)
                {
                    apiCall.MatchCount++;
                }

                if (apiCall.ReturnOnlyForNthMatch.GetValueOrDefault() == apiCall.MatchCount)
                {
                    return apiCall;
                }
            }
            return highScoreCalls.FirstOrDefault(c => c.ReturnOnlyForNthMatch.GetValueOrDefault() == 0);
        }

        private IList<MockApiCall> FindMatchingCalls(List<MockApiCall> apiCalls, HttpRequest request, string requestJson)
        {
            var highScore = 0;
            IList<MockApiCall> highScoreCalls = new List<MockApiCall>();
            foreach (MockApiCall apiCall in apiCalls)
            {
                if (apiCall.Expiry < DateTimeOffset.Now)
                    continue;

                var isMatch = true;
                var score = 0;

                var qMatch = CheckForQueryParameterMatch(request, apiCall);
                isMatch &= qMatch.Key;
                score += qMatch.Value;

                var hMatch = CheckForHeaderMatch(request, apiCall);
                isMatch &= hMatch.Key;
                score += hMatch.Value;

                var bMatch = CheckBodyPathMatch(requestJson, apiCall);
                isMatch &= bMatch.Key;
                score += bMatch.Value;

                if (isMatch && score > 0)
                {
                    if (score > highScore)
                    {
                        highScoreCalls.Clear();
                        highScoreCalls.Add(apiCall);
                        highScore = score;
                    }
                    else if (score == highScore)
                    {
                        highScoreCalls.Add(apiCall);
                    }
                }
            }

            return highScoreCalls;
        }

        private KeyValuePair<bool, int> CheckForQueryParameterMatch(HttpRequest request, MockApiCall apiCall)
        {
            if (apiCall.QueryParamsToMatch == null || apiCall.QueryParamsToMatch.Count == 0)
                return new KeyValuePair<bool, int>(true, 1);

            if (request.Query.Count == 0)
                return new KeyValuePair<bool, int>(false, 0);

            var matchQueryGroupList = apiCall.QueryParamsToMatch.GroupBy(q => q.Key).Select(g =>
                new KeyValuePair<string, List<string>>(g.Key, g.Select(gv => gv.Value).ToList())).ToList();

            var queryGroupList = request.Query.Select(q =>
                new KeyValuePair<string, List<string?>>(q.Key, q.Value.ToList())).ToList();

            return MatchGroupList(matchQueryGroupList, queryGroupList);
        }

        private KeyValuePair<bool,int> CheckForHeaderMatch(HttpRequest request, MockApiCall apiCall)
        {
            if (apiCall.HeadersToMatch == null || apiCall.HeadersToMatch.Count == 0)
                return new KeyValuePair<bool, int>(true, 1);

            if (request.Headers.Count == 0)
                return new KeyValuePair<bool, int>(false, 0);

            var matchQueryGroupList = apiCall.HeadersToMatch.GroupBy(q => q.Key).Select(g =>
                new KeyValuePair<string, List<string>>(g.Key, g.Select(gv => gv.Value).ToList())).ToList();

            var headerGroupList = request.Headers.Select(q =>
                new KeyValuePair<string, List<string?>>(q.Key, q.Value.ToList())).ToList();

            return MatchGroupList(matchQueryGroupList, headerGroupList);
        }

        private KeyValuePair<bool,int> CheckBodyPathMatch(string requestJson, MockApiCall apiCall)
        {
            if (apiCall.BodyPathsToMatch == null || apiCall.BodyPathsToMatch.Count == 0 )
                return new KeyValuePair<bool, int>(true, 1);

            if (string.IsNullOrWhiteSpace(requestJson))
                return new KeyValuePair<bool, int>(false, 0);

            var dynObj = JsonSerializer.Deserialize<JsonObject>(requestJson);

            var isMatch = true;
            var score = 0;
            foreach (var bodyPath in apiCall.BodyPathsToMatch)
            {
                var pathValue = GetPropertyValue(dynObj, bodyPath.Key);
                isMatch = isMatch && pathValue?.ToString() == bodyPath.Value;
                if (isMatch)
                    score += 10;
            }
            return new KeyValuePair<bool, int>(isMatch, isMatch ? score : 0);
        }

        public static JsonNode? GetPropertyValue(JsonNode? src, string propName)
        {
            if (propName.Contains("."))
            {
                var temp = propName.Split(new[] { '.' }, 2);
                return GetPropertyValue(GetPropertyValue(src, temp[0]), temp[1]);
            }
            else
            {
                return src?[propName];
            }
        }

        private KeyValuePair<bool,int> MatchGroupList(List<KeyValuePair<string, List<string>>> matchGroupList, List<KeyValuePair<string, List<string?>>> queryGroupList)
        {
            var isMatch = true;
            var score = 0;
            foreach (var matchGroup in matchGroupList)
            {
                isMatch = isMatch && queryGroupList.Any(q =>
                    q.Key == matchGroup.Key && q.Value.Any(v => v != null && matchGroup.Value.Contains(v)));
                if (isMatch)
                    score += 10;
            }

            return  new KeyValuePair<bool, int>(isMatch, isMatch ? score : 0);
        }
    }
}
