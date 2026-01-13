namespace Flex.Identity.Models.Users
{
    public class LoginResult
    {
        public LoginResult(string accessToken)
        {
            AccessToken = accessToken;
        }

        public string AccessToken { get; set; }
    }
}
