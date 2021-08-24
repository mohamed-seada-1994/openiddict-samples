namespace Matty.Client.ViewModels.Authorization
{
    public class DeviceCodeViewModel
    {
        public string DeviceCode { get; set; }
        public string UserCode { get; set; }
        public long? ExpiresIn { get; set; }
        public string VerificationUri { get; set; }
        public string VerificationUriComplete { get; set; }
    }
}
