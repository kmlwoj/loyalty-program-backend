namespace lojalBackend.Models
{
    public class UserDbModelOrg : UserDbModel
    {
        public string Organization { get; set; }
        public UserDbModelOrg(string login, string? email, AccountTypes type, int credits, DateTime? latestUpdate, string organization) : base(login, email, type, credits, latestUpdate)
        {
            Organization = organization;
        }
    }
}
