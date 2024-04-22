using Microsoft.AspNetCore.Authorization;

namespace lojalBackend
{
    public class RefreshRequirement : IAuthorizationRequirement
    {
        public RefreshRequirement()
        {
        }
    }
}
