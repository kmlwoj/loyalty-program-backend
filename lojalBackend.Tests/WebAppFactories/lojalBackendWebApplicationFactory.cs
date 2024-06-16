using System.IdentityModel.Tokens.Jwt;
using lojalBackend;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

public class lojalBackendWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the app's DbContext registration.
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LojClientDbContext>));
            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            var shopDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<LojShopDbContext>));
            if (shopDescriptor != null)
            {
                services.Remove(shopDescriptor);
            }

            // Add a database context (LojClientDbContext) using an in-memory database for testing.
            services.AddDbContext<LojClientDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryDbForTesting");
            });

            // Add a database context (LojShopDbContext) using an in-memory database for testing.
            services.AddDbContext<LojShopDbContext>(options =>
            {
                options.UseInMemoryDatabase("InMemoryShopDbForTesting");
            });

            // Build the service provider.
            var serviceProvider = services.BuildServiceProvider();

            // todo delete this below
            // Create the scope to obtain the services.
            using (var scope = serviceProvider.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var clientDb = scopedServices.GetRequiredService<LojClientDbContext>();
                var shopDb = scopedServices.GetRequiredService<LojShopDbContext>();

                // Ensure the database is created.
                clientDb.Database.EnsureCreated();
                shopDb.Database.EnsureCreated();
            }
        });

    }
}

