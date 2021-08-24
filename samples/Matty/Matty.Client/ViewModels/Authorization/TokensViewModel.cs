namespace Matty.Client.ViewModels.Authorization
{
    public class TokensViewModel
    {
        public string IdToken { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public long? ExpiresIn { get; set; }
    }
}
