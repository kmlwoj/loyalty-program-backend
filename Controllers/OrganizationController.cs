using DocumentFormat.OpenXml.Vml.Office;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
                var transaction = shopDbContext.Database.BeginTransaction();
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
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }
            else if(OrgTypes.Client.Equals(organization.Type))
            {
                var transaction = shopDbContext.Database.BeginTransaction();
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
            else
            {
                return BadRequest("Invalid organization type!");
            }
            return Ok("Organization added to the system!");
        }
    }
}
