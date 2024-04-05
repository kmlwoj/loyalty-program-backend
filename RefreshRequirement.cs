using Microsoft.AspNetCore.Authorization;

namespace lojalBackend
{
    public class RefreshRequirement : IAuthorizationRequirement
    {
        public string? ConnectionString { get; private set; }
        public RefreshRequirement(string? ConnStr)
        {
            ConnectionString = ConnStr;
        }
    }
}
