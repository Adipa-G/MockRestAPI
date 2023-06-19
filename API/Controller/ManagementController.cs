using System.Collections.Concurrent;
using System.Dynamic;

using API.Dto;
using API.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace API.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class ManagementController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;

        public ManagementController(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        [HttpPost("mock-call")]
        public async Task<OkObjectResult> RegisterMockCallAsync([FromBody]MockApiCallDto mockApiCall)
        {
            var cacheKey = $"{mockApiCall.ApiName.ToLower()}-{mockApiCall.Method.ToUpper()}-{mockApiCall.ApiPath.ToLower().TrimStart('/')}";
            var callId = Guid.NewGuid().ToString();
            var calls = _memoryCache.GetOrCreate(cacheKey, _ => new List<MockApiCall>());

            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            idMappings?.TryAdd(callId, cacheKey);

            var model = new MockApiCall(callId)
            {
                Response = mockApiCall.Response?.ToString(),
                ResponseCode = mockApiCall.ResponseCode,
                BodyPathsToMatch = mockApiCall.BodyPathsToMatch,
                HeadersToMatch = mockApiCall.HeadersToMatch,
                QueryParamsToMatch = mockApiCall.QueryParamsToMatch,
                ReturnOnlyForNthMatch = mockApiCall.ReturnOnlyForNthMatch,
                Expiry =
                DateTimeOffset.Now.Add(
                TimeSpan.FromSeconds(mockApiCall.TimeToLive.GetValueOrDefault(int.MaxValue)))
            };
            calls?.Add(model);

            var absoluteExpiration = (calls != null && calls.Any()) ? calls.Max(c => c.Expiry) : DateTimeOffset.Now;
            _memoryCache.Set(cacheKey, calls, absoluteExpiration);

            return await Task.FromResult(Ok(new {id = callId}));
        }

        [HttpDelete("mock-call/{callId}")]
        public StatusCodeResult DeleteMockCallAsync([FromRoute]string callId)
        {
            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            var mapping = idMappings?.FirstOrDefault(m => m.Key == callId);

            if (mapping.Equals(default(KeyValuePair<string,string>)))
            {
                return NotFound();
            }
            else
            {
                idMappings?.Remove(callId, out _);
                return Ok();
            }
        }

        [HttpGet("all-mock-calls")]
        public OkObjectResult GetAllMockCallsAsync()
        {
            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            var result = new ExpandoObject() as IDictionary<string,object?>;
            if (idMappings != null)
            {
                foreach (var mapping in idMappings)
                {
                    var mockApiCalls = _memoryCache.Get<List<MockApiCall>>(mapping.Value);
                    var apiTokens = GetApiPath(mapping.Value);
                    if (apiTokens == null || mockApiCalls == null)
                    {
                        continue;
                    }

                    var api = GetChildAddIfNotExists(result, apiTokens[0]);
                    var path = GetChildAddIfNotExists(api, apiTokens[2]);

                    var apiCallDtos = mockApiCalls.Where(mc => mc.Expiry > DateTimeOffset.Now).Select(mc => new MockApiCallDto
                    {
                        ApiName = apiTokens[0],
                        Method = apiTokens[1],
                        ApiPath = apiTokens[2],
                        BodyPathsToMatch = mc.BodyPathsToMatch,
                        HeadersToMatch = mc.HeadersToMatch,
                        QueryParamsToMatch = mc.QueryParamsToMatch,
                        ReturnOnlyForNthMatch = mc.ReturnOnlyForNthMatch,
                        Response = mc.Response != null ? JsonConvert.DeserializeObject<ExpandoObject>(mc.Response) : string.Empty,
                        ResponseCode = mc.ResponseCode,
                        TimeToLive = (int)(mc.Expiry - DateTimeOffset.Now).TotalSeconds
                    });
                    path.TryAdd(apiTokens[1], apiCallDtos);
                }
            }

            return new OkObjectResult(result);
        }

        private IDictionary<string, object?> GetChildAddIfNotExists(IDictionary<string, object?> parent, string childPath)
        {
            IDictionary<string, object?> child;
            if (parent.ContainsKey(childPath))
            {
                child = (IDictionary<string, object?>)parent[childPath]!;
            }
            else
            {
                child = new ExpandoObject();
                parent.Add(childPath, child);
            }
            return child;
        }

        private string[]? GetApiPath(string cacheKey)
        {
            var methods = new string[]
            {
                HttpMethods.Get, HttpMethods.Post, HttpMethods.Delete, HttpMethods.Head, HttpMethods.Options,
                HttpMethods.Trace, HttpMethods.Put, HttpMethods.Patch
            };
            foreach (string method in methods)
            {
                string splitter = $"-{method.ToUpper()}-";
                if (cacheKey.Contains(splitter))
                {
                    var tokens = cacheKey.Split(splitter);
                    return new string[] { tokens[0], method, tokens[1] };
                }
            }
            return null;
        }
    }
}
