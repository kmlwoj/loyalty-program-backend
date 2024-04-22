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
        private readonly IServiceProvider serviceProvider;
        public RefreshRequirementHandler(IHttpContextAccessor httpContextAccessor, IServiceProvider serviceProvider)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.serviceProvider = serviceProvider;
        }
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RefreshRequirement requirement)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                LojClientDbContext dbContext = scope.ServiceProvider.GetRequiredService<LojClientDbContext>();
                string? dbRefreshToken = null;
                Claim? login = context.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));

                if (login != null)
                {
                    dbRefreshToken = dbContext.RefreshTokens.FirstOrDefault(x => login.Value.Equals(x.Login))?.Token;
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
}
