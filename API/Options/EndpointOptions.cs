namespace API.Options
{
    public class EndpointOptions
    {
        public string RootFolderName { get; set; } = null!;
        public IList<EndpointOptionsApi> Apis { get; set; } = null!;
    }
}
