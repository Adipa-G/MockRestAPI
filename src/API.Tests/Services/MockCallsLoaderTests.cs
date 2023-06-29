using API.Options;
using API.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions;

using Microsoft.Extensions.Options;

using NSubstitute;

using Xunit;
using API.Tests.Helpers;
using API.Models;

using System.Collections.Concurrent;

using FluentAssertions;

namespace API.Tests.Services
{
    public class MockCallsLoaderTests
    {
        private readonly IOptions<ConfigOptions> _configOptions;
        private readonly IFileSystem _fileSystem;
        private readonly IMemoryCache _memoryCache;
        private readonly LoggerMock<MockCallsLoader> _logger;

        public MockCallsLoaderTests()
        {
            _configOptions = Substitute.For<IOptions<ConfigOptions>>();
            _fileSystem = Substitute.For<IFileSystem>();
            _memoryCache = Substitute.For<IMemoryCache>();
            _logger = new LoggerMock<MockCallsLoader>();
        }

        [Fact]
        public async Task GivenNoFilesInLocation_WhenLoadMockCallsAsync_ThenDoNothing()
        {
            //Arrange
            SetupFolder("mock-api-calls");

            //Act
            var sut = CreateSut();
            await sut.LoadMockCallsAsync();

            //Assert
            _logger.ReceivedOnce(LogLevel.Information, "Did not found any mock calls in the folder");
        }

        [Fact]
        public async Task GivenValidFile_WhenLoadMockCallsAsync_ThenLoadTheFile()
        {
            //Arrange
            var cacheKey = $"{"petstore".ToLower()}-{"GET".ToUpper()}-{"/pet/findByStatus".ToLower().TrimStart('/')}";
            var cache = new List<MockApiCall>();
            var mappings = new ConcurrentDictionary<string, string>();

            SetMemoryCache(cacheKey, cache);
            SetMemoryCache(Constants.IdMappingCacheKey, mappings);

            var json = await File.ReadAllTextAsync("Samples\\valid-mock-call.json");
            var folder = SetupFolder("mock-api-calls");
            SetupFile(folder.Name, new []{"a.json"}, new []{json});

            //Act
            var sut = CreateSut();
            await sut.LoadMockCallsAsync();

            //Assert
            cache.Count.Should().Be(1);
            cache[0].Should().BeEquivalentTo(new
            {
                CallId = "12439",
                ApiName = "petstore",
                ApiPath = "/pet/findByStatus",
                Method = "GET",
                QueryParamsToMatch = new List<KeyValuePair<string, string>>() { new("status", "sold") },
                TimeToLive = 360,
                ResponseCode = 200,
                ReturnOnlyForNthMatch = 2
            });
            Assert.NotStrictEqual(cache[0].Response.ToString(), @"{
                ""id"": 38,
                ""name"": ""Scooby Doo"",
                ""category"": {
                  ""id"": 1,
                  ""name"": ""Dogs""
                },
                ""status"": ""sold""
            }");

            mappings.Count.Should().Be(1);
            mappings.ContainsKey("12439").Should().BeTrue();
            mappings["12439"].Should().Be("petstore-GET-pet/findbystatus");
        }

        [Fact]
        public async Task GivenDuplicateEntries_WhenLoadMockCallsAsync_ThenLoadFirstAndSkipSecond()
        {
            //Arrange
            var cacheKey = $"{"petstore".ToLower()}-{"GET".ToUpper()}-{"/pet/findByStatus".ToLower().TrimStart('/')}";
            var cache = new List<MockApiCall>();
            var mappings = new ConcurrentDictionary<string, string>();

            SetMemoryCache(cacheKey, cache);
            SetMemoryCache(Constants.IdMappingCacheKey, mappings);

            var json = await File.ReadAllTextAsync("Samples\\duplicate-mock-call.json");
            var folder = SetupFolder("mock-api-calls");
            SetupFile(folder.Name, new[] { "a.json" }, new[] { json });

            //Act
            var sut = CreateSut();
            await sut.LoadMockCallsAsync();

            //Assert
            cache.Count.Should().Be(1);
            mappings.Count.Should().Be(1);
            _logger.ReceivedOnce(LogLevel.Error, "Ignoring the call");
        }

        private void SetupFile(string folderName,string[] fileNames, string[] fileContents)
        {
            var folder = SetupFolder(folderName);
            var files = new List<IFileInfo>();

            for (int i = 0; i < fileNames.Length; i++)
            {
                string fileName = fileNames[i];
                string fileContent = fileContents[i];

                var fileInfo = Substitute.For<IFileInfo>();
                fileInfo.FullName.Returns(fileName);
                files.Add(fileInfo);

                _fileSystem.File.ReadAllTextAsync(fileName).Returns(Task.FromResult(fileContent));
            }
            folder.GetFiles("*.json", SearchOption.AllDirectories).Returns(files.ToArray());
        }

        private IDirectoryInfo SetupFolder(string folderName)
        {
            var mockApiCallsFolder = Substitute.For<IDirectoryInfo>();
            mockApiCallsFolder.Name.Returns(folderName);
            var folders = new[] { mockApiCallsFolder };

            var appDomainFolder = Substitute.For<IDirectoryInfo>();
            appDomainFolder.GetDirectories().Returns(folders);

            _fileSystem.DirectoryInfo.New(Arg.Any<string>()).Returns(appDomainFolder);
            _configOptions.Value.Returns(new ConfigOptions() { MockApiCallsSubFolder = folderName });

            return mockApiCallsFolder;
        }

        private void SetMemoryCache<T>(string cacheKey, T entry)
        {
            _memoryCache.TryGetValue(cacheKey, out Arg.Any<T?>())
                .Returns(x =>
                {
                    x[1] = entry;
                    return true;
                });
        }

        private MockCallsLoader CreateSut()
        {
            return new MockCallsLoader(_configOptions, _fileSystem, _memoryCache, _logger);
        }
    }
}
