using System.Text;

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
        public async Task GivenStoredWithNoQueryParamsMatchHeaderMatchOrBodyMatch_WhenGenerateJsonResponseAsync_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
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
        public async Task GivenStoredWithQueryParameterMatch_WhenGenerateJsonResponseAsyncWithMatchingQueryParam_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddQueryParametersMatches(mockApiCall, "status", "available", "sold");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredWithQueryParameterMatch_WhenGenerateJsonResponseAsyncWithTwoQueryParamsWithOneMatching_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddQueryParametersMatches(mockApiCall, "status", "available", "sold");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");
            AddQueryParameters(context, "one", "two");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredWithOneAndTwoQueryParameterMatch_WhenGenerateJsonResponseAsyncWithBothMatching_ThenReturnHighestMatch()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCallOne = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = ""
            };
            AddQueryParametersMatches(mockApiCallOne, "status", "available", "sold");

            var mockApiCallTwo = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddQueryParametersMatches(mockApiCallTwo, "status", "available", "sold");
            AddQueryParametersMatches(mockApiCallTwo, "type", "dog", "cat");

            SetMemoryCache(cacheKey, mockApiCallOne, mockApiCallTwo);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");
            AddQueryParameters(context, "type", "cat");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredMatchTwoQueryParameters_WhenGenerateJsonResponseAsyncWithOneMatchingQueryParam_ThenReturnNotFound()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddQueryParametersMatches(mockApiCall, "status", "available", "sold");
            AddQueryParametersMatches(mockApiCall, "type", "dog", "cat");

            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(true);
            result.Key.Should().Be("404");
            result.Value.Should().Be("I have never met this man in my life.");
        }

        [Fact]
        public async Task GivenStoredWithHeaderMatch_WhenGenerateJsonResponseAsyncWithMatchingHeader_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddHeaderMatches(mockApiCall, "Authorization", "Bearer 123", "Bearer 234");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddHeaders(context, "Authorization", "Bearer 234");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredWithHeaderMatch_WhenGenerateJsonResponseAsyncWithTwoHeadersWithOneMatching_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddHeaderMatches(mockApiCall, "Authorization", "Bearer 123", "Bearer 234");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddHeaders(context, "Authorization", "Bearer 234");
            AddHeaders(context, "ClientId", "234");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredWithOneAndTwoHeadersMatch_WhenGenerateJsonResponseAsyncWithBothMatching_ThenReturnHighestMatch()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCallOne = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = ""
            };
            AddHeaderMatches(mockApiCallOne, "Authorization", "Bearer 123", "Bearer 234");
            SetMemoryCache(cacheKey, mockApiCallOne);

            var mockApiCallTwo = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddHeaderMatches(mockApiCallTwo, "Authorization", "Bearer 123", "Bearer 234");
            AddHeaderMatches(mockApiCallTwo, "ClientId", "123", "234");

            SetMemoryCache(cacheKey, mockApiCallOne, mockApiCallTwo);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddHeaders(context, "Authorization", "Bearer 234");
            AddHeaders(context, "ClientId", "123");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredMatchTwoHeaders_WhenGenerateJsonResponseAsyncWithOneMatchingHeader_ThenReturnNotFound()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddHeaderMatches(mockApiCall, "Authorization", "Bearer 123", "Bearer 234");
            AddHeaderMatches(mockApiCall, "ClientId", "123", "234");

            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddHeaders(context, "Authorization", "Bearer 123");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(true);
            result.Key.Should().Be("404");
            result.Value.Should().Be("I have never met this man in my life.");
        }

        //TODO
        [Fact]
        public async Task GivenStoredWithBodyPathMatch_WhenGenerateJsonResponseAsyncWithMatchingBodyPath_ThenReturnStoredResponse()
        {
            //Arrange
            string method = "post";
            string requestPath = "pet";
            var responseStatusCode = 200;
            var response = new
            {
                id = 38, 
                name = "Scooby Doo", 
                category = new { id = 1, name = "Dogs" }, 
                status = "available"
            };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddBodyMatches(mockApiCall, "category.id", "1");
            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            SetBody(context, new { category = new { id = 1, name = "Dogs" }});

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredWithOneAndTwoBodyPathMatch_WhenGenerateJsonResponseAsyncWithBothMatching_ThenReturnHighestMatch()
        {
            //Arrange
            string method = "post";
            string requestPath = "pet";
            var responseStatusCode = 200;
            var response = new
            {
                id = 38,
                name = "Scooby Doo",
                category = new { id = 1, name = "Dogs" },
                status = "available"
            };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCallOne = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = ""
            };
            AddBodyMatches(mockApiCallOne, "category.id", "1");
            SetMemoryCache(cacheKey, mockApiCallOne);

            var mockApiCallTwo = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddBodyMatches(mockApiCallTwo, "category.id", "1");
            AddBodyMatches(mockApiCallTwo, "status", "sold");
            SetMemoryCache(cacheKey, mockApiCallTwo, mockApiCallTwo);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            SetBody(context, new { category = new { id = 1, name = "Dogs" }, status = "sold" });

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(false);
            result.Key.Should().Be(responseStatusCode.ToString());
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenStoredMatchTwoBodyPaths_WhenGenerateJsonResponseAsyncWithOneMatchingBodyPath_ThenReturnNotFound()
        {
            //Arrange
            string method = "post";
            string requestPath = "pet";
            var responseStatusCode = 200;
            var response = new
            {
                id = 38,
                name = "Scooby Doo",
                category = new { id = 1, name = "Dogs" },
                status = "available"
            };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                Response = JsonConvert.SerializeObject(response)
            };
            AddBodyMatches(mockApiCall, "category.id", "1");
            AddBodyMatches(mockApiCall, "status", "sold");

            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            SetBody(context, new { category = new { id = 1, name = "Dogs" } });

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            await ExpectExampleResponseBuilderToReceiveCalls(true);
            result.Key.Should().Be("404");
            result.Value.Should().Be("I have never met this man in my life.");
        }

        [Fact]
        public async Task GivenStoredMatchForNthRequest_WhenGenerateJsonResponseAsyncWithMatch_ThenReturnWhenNthCall()
        {
            //Arrange
            string method = "get";
            string requestPath = "findByStatus";
            var responseStatusCode = 200;
            var response = new { id = "test" };

            var cacheKey = $"{ApiName.ToLower()}-{method.ToUpper()}-{requestPath.ToLower()}";

            var mockApiCall = new MockApiCall()
            {
                Expiry = DateTimeOffset.MaxValue,
                ResponseCode = responseStatusCode,
                ReturnOnlyForNthMatch = 2,
                Response = JsonConvert.SerializeObject(response)
            };
            AddQueryParametersMatches(mockApiCall, "status", "sold");

            SetMemoryCache(cacheKey, mockApiCall);

            var context = CreateContext(method, $"/{ApiName}/{requestPath}");
            AddQueryParameters(context, "status", "sold");

            //Act
            var sut = CreateSut();
            var result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            result.Key.Should().Be("404");
            result.Value.Should().Be("I have never met this man in my life.");

            //Act
            result = await sut.GenerateJsonResponseAsync(BaseUrl, ApiName, requestPath, context.Request);

            //Assert
            result.Key.Should().Be("200");
            result.Value.Should().Be(JsonConvert.SerializeObject(response));
        }

        [Fact]
        public async Task GivenNoMatchStoredButMatchExample_WhenGenerateJsonResponseAsync_ThenReturnResponse()
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
        public async Task GivenNoMatchStoredOrExamples_WhenGenerateJsonResponseAsync_ThenReturnErrorResponse()
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

        private void SetMemoryCache(string cacheKey, params MockApiCall[] mockApiCalls)
        {
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<List<MockApiCall>?>())
                .Returns(x =>
                {
                    x[1] = new List<MockApiCall>(mockApiCalls);
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

        private void AddHeaderMatches(MockApiCall mockApiCall, string name, params string[] values)
        {
            mockApiCall.HeadersToMatch ??= new List<KeyValuePair<string, string>>();
            foreach (string value in values)
            {
                mockApiCall.HeadersToMatch.Add(new KeyValuePair<string, string>(name, value));
            }
        }

        private void AddBodyMatches(MockApiCall mockApiCall, string name, string value)
        {
            mockApiCall.BodyPathsToMatch ??= new List<KeyValuePair<string, string>>();
            mockApiCall.BodyPathsToMatch.Add(new KeyValuePair<string, string>(name, value));
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

        private void AddHeaders(HttpContext context, string name, params string[] values)
        {
            context.Request.Headers.Add(name, new StringValues(values));
        }

        private void SetBody(HttpContext context, object value)
        {
            var updatedJson = JsonConvert.SerializeObject(value);
            context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(updatedJson));
        }

        private ResponseGeneratorService CreateSut()
        {
            return new ResponseGeneratorService(_logger, _memoryCache, _exampleResponseBuilder);
        }
    }
}
