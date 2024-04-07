using lojalBackend;
using lojalBackend.DbContexts.MainContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text;

namespace lojalBackend
{
    public class RefreshRequirementHandler : AuthorizationHandler<RefreshRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        public RefreshRequirementHandler(IHttpContextAccessor httpContextAccessor)
        {
            this.httpContextAccessor = httpContextAccessor;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RefreshRequirement requirement)
        {
            string? dbRefreshToken = null;
            using (LojClientDbContext db = new(requirement.ConnectionString))
            {
                Claim? login = context.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));

                if (login != null)
                {
                    dbRefreshToken = db.RefreshTokens.FirstOrDefault(x => login.Value.Equals(x.Login))?.Token;
                }
            }

            if (dbRefreshToken != null)
            {
                context.Succeed(requirement);
            }
            else
            {
                context.Fail();
                if (httpContextAccessor?.HttpContext is not null && !httpContextAccessor.HttpContext.Response.HasStarted)
                {
                    httpContextAccessor.HttpContext.Response.OnStarting(() =>
                    {
                        httpContextAccessor.HttpContext.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    });
                }
            }
            return Task.CompletedTask;
        }
    }
}
