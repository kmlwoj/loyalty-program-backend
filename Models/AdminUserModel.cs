using DydaktykaBackend.Models;
using System.Text.Json.Serialization;

namespace lojalBackend.Models
{
    public class AdminUserModel : UserModel
    {
        public string OrganizationName { get; set; }
        [JsonConstructor]
        public AdminUserModel(string username, string password) : base(username, password) { }
        public AdminUserModel(string username, string password, AccountTypes accountType, string? email, string organizationName) : base(username, password)
        {
            OrganizationName = organizationName;
            Email = email;
            AccountType = accountType;
        }
    }
}
