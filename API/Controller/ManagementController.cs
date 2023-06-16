using API.Dto;
using API.Models;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

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
        public async Task<StatusCodeResult> RegisterMockCallAsync([FromBody]MockApiCallDto mockApiCall)
        {
            var cacheKey = $"{mockApiCall.ApiName.ToLower()}-{mockApiCall.Method.ToUpper()}-{mockApiCall.ApiPath.ToLower()}";
            var calls = _memoryCache.GetOrCreate(cacheKey, _ => new List<MockApiCall>());

            var model = new MockApiCall()
            {
                Response = mockApiCall.Response,
                ResponseCode = mockApiCall.ResponseCode,
                BodyPathsToMatch = mockApiCall.BodyPathsToMatch,
                HeadersToMatch = mockApiCall.HeadersToMatch,
                QueryParamsToMatch = mockApiCall.QueryParamsToMatch,
                Expiry =
                DateTimeOffset.Now.Add(
                TimeSpan.FromSeconds(mockApiCall.TimeToLive.GetValueOrDefault(int.MaxValue)))
            };
            calls?.Add(model);

            var absoluteExpiration = (calls != null && calls.Any()) ? calls.Max(c => c.Expiry) : DateTimeOffset.Now;
            _memoryCache.Set(cacheKey, calls, absoluteExpiration);

            return await Task.FromResult(Ok());
        }
    }
}
