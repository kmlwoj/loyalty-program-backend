using System.Net;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace lojalBackend.Models
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccountTypes
    {
        Administrator,
        Manager,
        Worker
    }
    public class UserModel
    {
        public string Username { get; set; }
        public string Password { get; set; }
        private SecureString SecuredPassword;
        public AccountTypes? AccountType { get; set; }
        public string? Email { get; set; }
        [JsonConstructor]
        public UserModel(string username, string password)
        {
            Username = username;
            Password = password;
            SecurePassword();
            AccountType = AccountTypes.Worker;
        }
        public bool IsPassword()
        {
            return !string.IsNullOrEmpty(new NetworkCredential("", SecuredPassword).Password);
        }
        public void SecurePassword()
        {
            SecuredPassword = new NetworkCredential("", Password).SecurePassword;
            Password = "";
        }
        public SecureString GetSecurePassword() => SecuredPassword;
        public string ConvertFromEnum()
        {
            switch (AccountType)
            {
                case AccountTypes.Administrator: return "Administrator";
                case AccountTypes.Manager: return "Manager";
                case AccountTypes.Worker: return "Worker";
                default: return "";
            }
        }
        public static AccountTypes ConvertToEnum(string type)
        {
            switch (type)
            {
                case "Administrator": return AccountTypes.Administrator;
                case "Manager": return AccountTypes.Manager;
                case "Worker": return AccountTypes.Worker;
                default: return AccountTypes.Worker;
            }
        }
        public byte[] EncryptPassword()
        {
            byte[] salt = RandomNumberGenerator.GetBytes(64);

            Password = Convert.ToHexString(Rfc2898DeriveBytes.Pbkdf2(
                Encoding.UTF8.GetBytes(new NetworkCredential("", SecuredPassword).Password),
                salt,
                350000,
                HashAlgorithmName.SHA512,
                64
                ));

            return salt;
        }
        public bool VerifyPassword(SecureString password, string salt)
        {
            var hashToCompare = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(new NetworkCredential("", password).Password), Convert.FromHexString(salt), 350000, HashAlgorithmName.SHA512, 64);

            return CryptographicOperations.FixedTimeEquals(hashToCompare, Convert.FromHexString(new NetworkCredential("", SecuredPassword).Password));
        }
    }
}
