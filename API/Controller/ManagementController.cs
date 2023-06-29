using System.Collections.Concurrent;
using System.Dynamic;

using API.Models;
using API.Services;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace API.Controller
{
    [ApiController]
    [Route("[controller]")]
    public class ManagementController : ControllerBase
    {
        private readonly IMemoryCache _memoryCache;
        private readonly IMockCallsLoader _mockCallsLoader;

        public ManagementController(IMemoryCache memoryCache, IMockCallsLoader mockCallsLoader)
        {
            _memoryCache = memoryCache;
            _mockCallsLoader = mockCallsLoader;
        }

        [HttpGet("mock-call/{callId}")]
        public ActionResult GetMockCall([FromRoute] string callId)
        {
            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            var cacheKey = idMappings != null && idMappings.ContainsKey(callId) ? idMappings?[callId] : null;
            var calls = cacheKey != null ? _memoryCache.GetOrCreate(cacheKey!, _ => new List<MockApiCall>()) : new List<MockApiCall>();
            var call = calls?.FirstOrDefault(c => c.CallId == callId);
            if (call == null)
            {
                return NotFound();
            }
            return Ok(call);
        }

        [HttpPost("mock-call/{callId}")]
        public ActionResult RegisterMockCall([FromRoute] string callId, [FromBody]MockApiCall mockApiCall)
        {
            var cacheKey = $"{mockApiCall.ApiName.ToLower()}-{mockApiCall.Method.ToUpper()}-{mockApiCall.ApiPath.ToLower().TrimStart('/')}";
            var calls = _memoryCache.GetOrCreate(cacheKey, _ => new List<MockApiCall>());
            var call = calls?.FirstOrDefault(c => c.CallId == callId);
            if (call != null)
            {
                calls?.Remove(call);
            }
            
            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            idMappings?.TryAdd(callId, cacheKey);

            mockApiCall.CallId = callId;
            mockApiCall.Expiry = DateTimeOffset.Now.Add(TimeSpan.FromSeconds(mockApiCall.TimeToLive.GetValueOrDefault(int.MaxValue)));
            calls?.Add(mockApiCall);

            var absoluteExpiration = (calls != null && calls.Any()) ? calls.Max(c => c.Expiry) : DateTimeOffset.Now;
            _memoryCache.Set(cacheKey, calls, absoluteExpiration);

            return Ok(new {id = callId});
        }

        [HttpDelete("mock-call/{callId}")]
        public ActionResult RemoveMockCall([FromRoute]string callId)
        {
            var idMappings = _memoryCache.Get<ConcurrentDictionary<string, string>>(Constants.IdMappingCacheKey);
            var cacheKey = idMappings != null && idMappings.ContainsKey(callId) ? idMappings?[callId] : null;
            var calls = cacheKey != null ? _memoryCache.GetOrCreate(cacheKey!, _ => new List<MockApiCall>()) : new List<MockApiCall>();
            var call = calls?.FirstOrDefault(c => c.CallId == callId);
            if (call == null)
            {
                return NotFound();
            }
            calls?.Remove(call);
            idMappings?.Remove(callId, out _);
            return Ok(new { id = callId });
        }

        [HttpGet("mock-calls")]
        public OkObjectResult GetAllMockCalls()
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
                    path.TryAdd(apiTokens[1], mockApiCalls);
                }
            }

            return new OkObjectResult(result);
        }

        [HttpPost("reset")]
        public async Task<OkResult> Reset()
        {
            var cache = _memoryCache as MemoryCache;
            cache?.Clear();

            var idMappings = new ConcurrentDictionary<string, string>();
            _memoryCache?.Set(Constants.IdMappingCacheKey, idMappings);
            await _mockCallsLoader.LoadMockCallsAsync();

            return new OkResult();
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
