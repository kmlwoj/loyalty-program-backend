using DocumentFormat.OpenXml.Vml.Office;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;

namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
    public class OrganizationController : ControllerBase
    {
        private readonly ILogger<OrganizationController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public OrganizationController(ILogger<OrganizationController> logger, IConfiguration configuration, LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }
        /// <summary>
        /// Retrieves organizations with types
        /// </summary>
        /// <returns>List of objects of OrganizationModel schema</returns>
        [HttpGet("GetOrganizations")]
        public async Task<IActionResult> GetOrganizations()
        {
            List<OrganizationModel> answer = new();
            await foreach(var entry in clientDbContext.Organizations.AsAsyncEnumerable())
            {
                _ = Enum.TryParse(entry.Type, out OrgTypes org);
                answer.Add(new(entry.Name, org));
            }
            return answer.Count > 0 ? new JsonResult(answer) : NotFound("No organizations found in the system!");
        }
        /// <summary>
        /// Adds new organization to the system
        /// </summary>
        /// <param name="organization">Organization data</param>
        [HttpPost("AddOrganization")]
        public async Task<IActionResult> AddOrganization([FromBody] OrganizationModel organization)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(organization.Name);
            if (checkOrg != null)
                return BadRequest("Requested organization already exists in the system!");
            checkOrg = new()
            {
                Name = organization.Name,
                Type = Enum.GetName(typeof(OrgTypes), organization.Type) ?? "Client"
            };

            if (OrgTypes.Shop.Equals(organization.Type))
            {
                using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
                using(var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        DbContexts.ShopContext.Organization shopOrg = new()
                        {
                            Name = organization.Name
                        };
                        shopDbContext.Organizations.Add(shopOrg);
                        clientDbContext.Organizations.Add(checkOrg);

                        await shopDbContext.SaveChangesAsync();
                        await clientDbContext.SaveChangesAsync();
                    }
                    catch
                    {
                        clientTransaction.Rollback();
                        shopTransaction.Rollback();
                        throw;
                    }
                    await clientTransaction.CommitAsync();
                    await shopTransaction.CommitAsync();
                }
            }
            else if(OrgTypes.Client.Equals(organization.Type))
            {
                using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
                {
                    try
                    {
                        clientDbContext.Organizations.Add(checkOrg);

                        await clientDbContext.SaveChangesAsync();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    await transaction.CommitAsync();
                }
            }
            else
            {
                return BadRequest("Invalid organization type!");
            }
            return Ok("Organization added to the system!");
        }
        /// <summary>
        /// Deletes a given organization from the system
        /// </summary>
        /// <param name="organization">Targeted organization</param>
        [HttpDelete("DeleteOrganization")]
        public async Task<IActionResult> DeleteOrganization([FromBody] string organization)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(organization);
            if (checkOrg == null)
                return NotFound("Requested organization was not found in the system!");
            using (var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
            using (var clientTransaciton = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    Task shopTask = Task.Run(async () =>
                    {
                        foreach (var offer in await shopDbContext.Offers.Where(x => organization.Equals(x.Organization)).ToListAsync())
                        {
                            var discounts = shopDbContext.Discounts.Where(x => offer.OfferId.Equals(x.OfferId));
                            if (discounts != null)
                                shopDbContext.Discounts.RemoveRange(discounts);

                            var codes = shopDbContext.Codes.Where(x => offer.OfferId.Equals(x.OfferId));
                            if (codes != null)
                                shopDbContext.Codes.RemoveRange(codes);

                            shopDbContext.Offers.Remove(offer);
                        }
                        DbContexts.ShopContext.Organization? shopOrg = await shopDbContext.Organizations.FindAsync(organization);
                        if (shopOrg != null)
                            shopDbContext.Organizations.Remove(shopOrg);

                        await shopDbContext.SaveChangesAsync();
                    });
                    Task clientTask = Task.Run(async () =>
                    {
                        //TODO: check if organization has any offers in client db, if not then delete
                        var users = await clientDbContext.Users.Where(x => organization.Equals(x.Organization)).ToListAsync();

                        foreach (var user in users)
                        {
                            RefreshToken? token = await clientDbContext.RefreshTokens.FindAsync(user.Login);
                            if (token != null)
                                clientDbContext.RefreshTokens.Remove(token);
                        }

                        var transactions = clientDbContext.Transactions.Where(x => organization.Equals(x.Shop));
                        if (transactions != null)
                            clientDbContext.Transactions.RemoveRange(transactions);

                        if (users != null)
                            clientDbContext.Users.RemoveRange(users);

                        foreach (var offer in await clientDbContext.Offers.Where(x => organization.Equals(x.Organization)).ToListAsync())
                        {
                            var discounts = clientDbContext.Discounts.Where(x => offer.OfferId.Equals(x.OfferId));
                            if (discounts != null)
                                clientDbContext.Discounts.RemoveRange(discounts);

                            var codes = clientDbContext.Codes.Where(x => offer.OfferId.Equals(x.OfferId));
                            if (codes != null)
                                clientDbContext.Codes.RemoveRange(codes);

                            clientDbContext.Offers.Remove(offer);
                        }
                        DbContexts.MainContext.Organization? clientOrg = await clientDbContext.Organizations.FindAsync(organization);
                        if (clientOrg != null)
                            clientDbContext.Organizations.Remove(clientOrg);

                        await clientDbContext.SaveChangesAsync();
                    });
                    await Task.WhenAll(shopTask, clientTask);
                }
                catch
                {
                    shopTransaction.Rollback();
                    clientTransaciton.Rollback();
                    throw;
                }
                await shopTransaction.CommitAsync();
                await clientTransaciton.CommitAsync();
            }
            return Ok("Organization deleted!");
        }
    }
}
