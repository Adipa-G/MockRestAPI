namespace API.Models
{
    public class MockApiCall
    {
        public MockApiCall(string callId)
        {
            CallId = callId;
        }

        public string CallId { get; }
        public IList<KeyValuePair<string, string>>? QueryParamsToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? HeadersToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? BodyPathsToMatch { get; set; }
        public DateTimeOffset Expiry { get; set; }
        public int ResponseCode { get; set; }
        public string? Response { get; set; }
        public int? ReturnOnlyForNthMatch { get; set; }
        public int MatchCount { get; set; }
    }
}
