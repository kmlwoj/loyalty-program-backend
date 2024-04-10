using DydaktykaBackend.Models;

namespace lojalBackend.Models
{
    public class UserDbModel
    {
        public string Login { get; set; }
        public AccountTypes Type { get; set; }
        public int Credits { get; set; }
        public DateTime? LatestUpdate { get; set; }
        public UserDbModel()
        {
            Login = string.Empty;
            Credits = 0;
            Type = AccountTypes.Worker;
            LatestUpdate = DateTime.MinValue;
        }
        public UserDbModel(string login, AccountTypes type, int credits, DateTime? latestUpdate)
        {
            Login = login;
            Type = type;
            Credits = credits;
            LatestUpdate = latestUpdate;
        }
    }
}
