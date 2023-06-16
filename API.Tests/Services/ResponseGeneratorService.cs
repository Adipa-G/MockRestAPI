using API.Models;
using API.Services;
using API.Tests.Helpers;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

using Newtonsoft.Json;

using NSubstitute;

using Xunit;

namespace API.Tests.Services
{
    public class ResponseGeneratorServiceTests
    {
        private const string BaseUrl = "http://localhost";
        private const string ApiName = "mockApi";

        private readonly LoggerMock<ResponseGeneratorService> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly ISwaggerExampleResponseBuilderService _exampleResponseBuilder;

        public ResponseGeneratorServiceTests()
        {
            _logger = Substitute.For<LoggerMock<ResponseGeneratorService>>();
            _memoryCache = Substitute.For<IMemoryCache>();
            _exampleResponseBuilder = Substitute.For<ISwaggerExampleResponseBuilderService>();
        }

        [Fact]
        public async Task GivenMockAPIRequestMatchSingleQueryInStoredResponses_WhenGenerateJsonResponseAsync_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";
            
            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue, ResponseCode = 200, Response = response
            };
            AddQueryParametersMatches(mockApiCall, "status", "available");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "available");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenMockAPIRequestNotMatchSingleQueryInStoredResponses_WhenGenerateJsonResponseAsync_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue
            };
            AddQueryParametersMatches(mockApiCall, "status", "available");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");

            //Act
            var sut = CreateSut();
            await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(true);
        }
        
        [Fact]
        public async Task GivenMockAPIRequestGetsResponseFromExampleResponseBuilder_WhenGenerateJsonResponseAsync_ThenReturnResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "pet/21";
            string responseStatusCode = "200";
            string responsePayload = "{ \"id\" = \"test\" }";

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            _exampleResponseBuilder.GetResponse(BaseUrl, ApiName, requestPath, context.Request)
                .Returns(new KeyValuePair<string, string?>(responseStatusCode, responsePayload));

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            result.Should().NotBeEquivalentTo(default(KeyValuePair<string, string?>));
            await _exampleResponseBuilder.Received(1).GetResponse(BaseUrl, ApiName,requestPath, context.Request);
            result.Key.Should().Be(responseStatusCode);
            result.Value.Should().Be(responsePayload);
        }

        [Fact]
        public async Task GivenMockAPIRequestDoesNotHitAny_WhenGenerateJsonResponseAsync_ThenReturnErrorResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "pet/21";

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            result.Should().NotBeEquivalentTo(default(KeyValuePair<string, string?>));
            await _exampleResponseBuilder.Received(1).GetResponse(BaseUrl, ApiName, requestPath, context.Request);
            result.Key.Should().Be("404");
            result.Value.Should().Be("I have never met this man in my life.");
        }

        private async Task ExpectExampleResponseBuilderToReceiveCalls(bool toReceive)
        {
            await _exampleResponseBuilder.Received(toReceive ? 1 : 0).GetResponse(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<string>(), Arg.Any<HttpRequest>());
        }

        private void SetMemoryCache(string cacheKey, MockApiCall mockApiCall)
        {
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<List<MockApiCall>?>())
                .Returns(x =>
                {
                    x[1] = new List<MockApiCall>() { mockApiCall };
                    return true;
                });
        }

        private void AddQueryParametersMatches(MockApiCall mockApiCall, string name, params string[] values)
        {
            mockApiCall.QueryParamsToMatch ??= new List<KeyValuePair<string, string>>();
            foreach (string value in values)
            {
                mockApiCall.QueryParamsToMatch.Add(new KeyValuePair<string, string>(name, value));
            }
        }

        private HttpContext CreateContext(string method, string path)
        {
            var httpContext = new DefaultHttpContext();
            var request = httpContext.Request;
            request.Scheme = "http";
            request.Host = new HostString("localhost");
            request.Method = method;
            request.Path = new PathString(path);
            request.ContentType = "application/json";

            return httpContext;
        }

        private void AddQueryParameters(HttpContext context, string name, params string[] values)
        {
            List<KeyValuePair<string, List<string?>>> queryGroupList = new();
            if (context.Request.Query.Count > 0)
            {
                queryGroupList = context.Request.Query.Select(q =>
                    new KeyValuePair<string, List<string?>>(q.Key, q.Value.ToList())).ToList();
            }
            queryGroupList.Add(new KeyValuePair<string, List<string?>>(name, new List<string?>(values)));
            var dictionary = queryGroupList
                .Select(g => new KeyValuePair<string, StringValues>(g.Key, new StringValues(g.Value.ToArray())))
                .ToDictionary(l => l.Key, l => l.Value);
            context.Request.Query = new QueryCollection(dictionary);
        }

        private ResponseGeneratorService CreateSut()
        {
            return new ResponseGeneratorService(_logger, _memoryCache, _exampleResponseBuilder);
        }
    }
}
