namespace API.Dto
{
    public class MockApiCall
    {
        public string ApiName { get; set; } = null!;
        public string ApiPath { get; set; } = null!;
        public string Method { get; set; } = null!;
        public IList<KeyValuePair<string, string>>? QueryParamsToMatch { get; set; }
        public IList<KeyValuePair<string,string>>? HeadersToMatch { get; set; }
        public IList<KeyValuePair<string,string>>? BodyPathsToMatch { get; set; }
        public long? TimeToLive { get; set; }
        public dynamic? Response { get; set; }
    }
}
