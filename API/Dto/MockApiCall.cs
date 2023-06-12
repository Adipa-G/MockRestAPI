namespace API.Dto
{
    public class MockApiCall
    {
        public string? ApiName { get; set; }
        public string? ApiPath { get; set; }
        public string? Method { get; set; }
        public string[]? QueryParamsToMatch { get; set; }
        public KeyValuePair<string,string>? HeadersToMatch { get; set; }
        public KeyValuePair<string,string>? BodyPathsToMatch { get; set; }
        public dynamic? Response { get; set; }
    }
}
