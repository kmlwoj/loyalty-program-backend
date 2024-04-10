using DydaktykaBackend.Models;

namespace lojalBackend.Models
{
    public class AdminUserModel : UserModel
    {
        public string OrganizationName { get; set; }
        public AdminUserModel(string username, string password, AccountTypes accountType, string? email, string organizationName) : base(username, password)
        {
            OrganizationName = organizationName;
            Email = email;
            AccountType = accountType;
        }
    }
}
