using System.Text.Json.Serialization;

namespace API.Models
{
    public class MockApiCall
    {
        public string? CallId { get; set; } = null!;
        public string ApiName { get; set; } = null!;
        public string ApiPath { get; set; } = null!;
        public string Method { get; set; } = null!;
        public IList<KeyValuePair<string, string>>? QueryParamsToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? HeadersToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? BodyPathsToMatch { get; set; }
        public long? TimeToLive { get; set; }
        public int ResponseCode { get; set; }
        public dynamic? Response { get; set; }
        public int? ReturnOnlyForNthMatch { get; set; }
        public int MatchCount { get; set; }

        [JsonIgnore]
        public DateTimeOffset Expiry { get; set; }
    }
}
