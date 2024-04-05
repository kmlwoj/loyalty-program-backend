using DydaktykaBackend.Models;
using System.Net;
using System.Security;

namespace lojalBackend.Models
{
    public class LoginModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        private SecureString SecuredPassword;
        public LoginModel(string username, string password)
        {
            Username = username;
            Password = password;
            SecurePassword();
        }
        public void SecurePassword()
        {
            SecuredPassword = new NetworkCredential("", Password).SecurePassword;
            Password = "";
        }
        public SecureString GetSecurePassword() => SecuredPassword;
    }
}
