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
    public class OfferController : ControllerBase
    {
        private readonly ILogger<OfferController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public OfferController(ILogger<OfferController> logger, IConfiguration configuration, LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }
        /// <summary>
        /// Retrieves offers from a given organization
        /// </summary>
        /// <param name="organization">Targeted organization</param>
        /// <returns>List of offers of OfferModel schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetOffers/{organization}")]
        public async Task<IActionResult> GetOffers(string organization)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(organization);
            if (checkOrg == null)
                return NotFound("Requested organization was not found in the system!");
            if (!checkOrg.Type.Equals(Enum.GetName(typeof(OrgTypes), OrgTypes.Shop)))
                return BadRequest("Organization is not a registered shop!");

            List<OfferModel> answer = new();
            foreach (var entry in await shopDbContext.Offers.Where(x => organization.Equals(x.Organization)).ToListAsync())
            {
                DbContexts.ShopContext.Discount? discount = await shopDbContext.Discounts.Where(x => entry.OfferId.Equals(x.OfferId) && x.Expiry.CompareTo(DateTime.UtcNow) > 0).FirstOrDefaultAsync();
                answer.Add(new(
                    entry.Name,
                    entry.Price,
                    organization,
                    entry.State > 0, //mysql ef bug
                    entry.Category,
                    discount != null ? new(
                        discount.Name,
                        discount.Reduction,
                        discount.Expiry,
                        entry.Price
                        ) : null
                    ));
            }
            return new JsonResult(answer);
        }
    }
}
