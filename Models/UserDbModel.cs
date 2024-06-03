namespace lojalBackend.Models
{
    public class UserDbModel
    {
        public string Login { get; set; }
        public string? Email { get; set; }
        public AccountTypes Type { get; set; }
        public int Credits { get; set; }
        public DateTime? LatestUpdate { get; set; }
        public UserDbModel()
        {
            Login = string.Empty;
            Email = null;
            Credits = 0;
            Type = AccountTypes.Worker;
            LatestUpdate = DateTime.MinValue;
        }
        public UserDbModel(string login, string? email, AccountTypes type, int credits, DateTime? latestUpdate)
        {
            Login = login;
            Email = email;
            Type = type;
            Credits = credits;
            LatestUpdate = latestUpdate;
        }
    }
}
