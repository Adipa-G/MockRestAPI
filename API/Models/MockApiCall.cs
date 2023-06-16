namespace API.Models
{
    public class MockApiCall
    {
        public IList<KeyValuePair<string, string>>? QueryParamsToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? HeadersToMatch { get; set; }
        public IList<KeyValuePair<string, string>>? BodyPathsToMatch { get; set; }
        public DateTimeOffset Expiry { get; set; }
        public int ResponseCode { get; set; }
        public dynamic? Response { get; set; }
    }
}
