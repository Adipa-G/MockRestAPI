namespace API.Options
{
    public class ConfigOptions
    {
        public string RootFolderName { get; set; } = null!;
        public string MockApiCallsSubFolder { get; set; } = null!;
        public IList<EndpointOptionsApi> Apis { get; set; } = null!;
    }
}
