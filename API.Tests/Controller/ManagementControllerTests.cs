using System.Collections.Concurrent;

using API.Controller;
using API.Models;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using NSubstitute;

using Xunit;

namespace API.Tests.Controller
{
    public class ManagementControllerTests
    {
        private IMemoryCache _memoryCache;

        public ManagementControllerTests()
        {
            _memoryCache = Substitute.For<IMemoryCache>();
        }

        [Fact]
        public void GivenAMockCall_WhenGetMockCall_ThenReturn()
        {
            //Arrange
            var callId = "121232";
            var cacheKey = "xapi-GET-pet/13";
            var call = new MockApiCall() { CallId = callId };
            CacheApiMappings(callId, cacheKey);
            CacheApiCalls(cacheKey, call);

            //Act
            var sut = CreateSut();
            var result = sut.GetMockCall(callId) as OkObjectResult;

            //Assert
            result.Should().NotBeNull();
            result?.Value.Should().BeEquivalentTo(call);
        }

        [Fact]
        public void GivenAMockCall_WhenRegisterMockCall_ThenStore()
        {
            //Arrange
            var callId = "121232";
            var call = new MockApiCall() { ApiName = "api", Method = "post", ApiPath = "pet/12", CallId = callId };
            
            //Act
            var sut = CreateSut();
            var result = sut.RegisterMockCall(callId, call) as OkObjectResult;

            //Assert
            result.Should().NotBeNull();
            result?.Value.Should().BeEquivalentTo(new {id = callId});
        }

        [Fact]
        public void GivenAMockCall_WhenRemoveMockCall_ThenRemove()
        {
            //Arrange
            var callId = "121232";
            var cacheKey = "xapi-GET-pet/13";
            var call = new MockApiCall() { CallId = callId };
            CacheApiMappings(callId, cacheKey);
            CacheApiCalls(cacheKey, call);

            //Act
            var sut = CreateSut();
            var result = sut.RemoveMockCall(callId) as OkObjectResult;

            //Assert
            result.Should().NotBeNull();
            result?.Value.Should().BeEquivalentTo(new { id = callId });
        }

        [Fact]
        public void GivenAMockCall_WhenGetAllMockCalls_ThenReturnAll()
        {
            //Arrange
            var callId = "121232";
            var cacheKey = "xapi-GET-pets";
            var call = new MockApiCall() { CallId = callId };
            CacheApiMappings(callId, cacheKey);
            CacheApiCalls(cacheKey, call);

            //Act
            var sut = CreateSut();
            var result = sut.GetAllMockCalls() as OkObjectResult;

            //Assert
            result.Should().NotBeNull();
            var expect = new { xapi = new { pets = new { GET = new[] { call } } } };

            var resultJson = JsonConvert.SerializeObject(result.Value);
            var expectedJson = JsonConvert.SerializeObject(expect);
            resultJson.Should().Be(expectedJson);
        }

        private void CacheApiMappings(string callId, string cacheKey)
        {
            _memoryCache.TryGetValue(Constants.IdMappingCacheKey, out Arg.Any<ConcurrentDictionary<string,string>?>())
                .Returns(x =>
                {
                    x[1] = new ConcurrentDictionary<string,string>(new []{new KeyValuePair<string, string>(callId, cacheKey)});
                    return true;
                });
        }

        private void CacheApiCalls(string cacheKey, params MockApiCall[] mockApiCalls)
        {
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<List<MockApiCall>?>())
                .Returns(x =>
                {
                    x[1] = new List<MockApiCall>(mockApiCalls);
                    return true;
                });
        }

        private ManagementController CreateSut()
        {
            return new ManagementController(_memoryCache);
        }
    }
}
