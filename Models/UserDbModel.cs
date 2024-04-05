namespace DydaktykaBackend.Models
{
    public class UserDbModel
    {
        public string Login { get; set; }
        public AccountTypes? AccountType { get; set; }
        public string? Email { get; set; }
        public string? OrganizationName { get; set; }
        public UserDbModel()
        {
            Login = "";
        }
        public UserDbModel(string login, AccountTypes? accountType, string? email, string? organizationName)
        {
            Login = login;
            AccountType = accountType;
            Email = email;
            OrganizationName = organizationName;
        }
    }
}
