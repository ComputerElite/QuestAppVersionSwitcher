namespace OculusGraphQLApiLib.Results
{
    public class ReleaseChannel
    {
        public string id { get; set; } = "";
        public string channel_name { get; set; } = "";
        public AndroidBinary latest_supported_binary { get; set; } = new AndroidBinary();
        
    }
}