using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static lojalBackend.ImageManager;

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
                DbContexts.ShopContext.Discount? discount = await shopDbContext.Discounts
                    .Where(x => entry.OfferId.Equals(x.OfferId) && x.Expiry.CompareTo(DateTime.UtcNow) > 0)
                    .FirstOrDefaultAsync();
                answer.Add(new(
                    entry.OfferId,
                    entry.Name,
                    entry.Price,
                    organization,
                    entry.State > 0, //mysql ef bug
                    entry.Category,
                    discount != null ? new(
                        discount.OfferId,
                        discount.Name,
                        discount.Reduction,
                        discount.Expiry,
                        entry.Price
                        ) : null
                    ));
            }
            return new JsonResult(answer);
        }
        /// <summary>
        /// Adds new offer to the system
        /// </summary>
        /// <param name="offer">New offer</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("AddOffer")]
        public async Task<IActionResult> AddOffer([FromBody] OfferModel offer)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(offer.Organization);
            if (checkOrg == null)
                return NotFound("Requested organization was not found in the system!");
            if (!checkOrg.Type.Equals(Enum.GetName(typeof(OrgTypes), OrgTypes.Shop)))
                return BadRequest("Organization is not a registered shop!");
            if (offer.Category != null)
            {
                DbContexts.MainContext.Category? checkCat = await clientDbContext.Categories.FindAsync(offer.Category);
                if (checkCat == null)
                    return NotFound("Requested category was not found in the system!");
            }
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers
                .Where(x => x.Organization.Equals(offer.Organization) && x.Name.Equals(offer.Name) && x.Price.Equals(offer.Price))
                .FirstOrDefaultAsync();
            if (checkOffer != null)
                return BadRequest("Offer already exists in the system!");

            int newID = -1;
            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    DbContexts.ShopContext.Offer newOffer = new()
                    {
                        Name = offer.Name,
                        Price = offer.Price,
                        Category = offer.Category,
                        Organization = offer.Organization,
                        State = (ulong)(offer.IsActive ? 1 : 0)
                    };
                    shopDbContext.Offers.Add(newOffer);

                    await shopDbContext.SaveChangesAsync();
                    newID = newOffer.OfferId;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok(newID);
        }
        /// <summary>
        /// Changes details of an offer
        /// </summary>
        /// <param name="offer">Offer object</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPut("ChangeOffer")]
        public async Task<IActionResult> ChangeOffer([FromBody] OfferModel offer)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(offer.Organization);
            if (checkOrg == null)
                return NotFound("Requested organization was not found in the system!");
            if (!checkOrg.Type.Equals(Enum.GetName(typeof(OrgTypes), OrgTypes.Shop)))
                return BadRequest("Organization is not a registered shop!");
            if (offer.Category != null)
            {
                DbContexts.MainContext.Category? checkCat = await clientDbContext.Categories.FindAsync(offer.Category);
                if (checkCat == null)
                    return NotFound("Requested category was not found in the system!");
            }
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers
                .Where(x => x.OfferId.Equals(offer.ID))
                .FirstOrDefaultAsync();
            if (checkOffer == null)
                return NotFound("Offer was not found in the system!");

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    checkOffer.State = (ulong)(offer.IsActive ? 1 : 0);
                    checkOffer.Category = offer.Category;
                    checkOffer.Price = offer.Price;
                    checkOffer.Name = offer.Name;
                    shopDbContext.Update(checkOffer);

                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }
            return Ok("Offer changed!");
        }
        /// <summary>
        /// Deletes targeted offer
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpDelete("DeleteOffer/{ID:int}")]
        public async Task<IActionResult> DeleteOffer(int ID)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null) 
                return NotFound("Offer with the given ID was not found!");

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var discounts = await shopDbContext.Discounts.Where(x => x.OfferId == ID).ToListAsync();
                    if(discounts != null)
                        shopDbContext.Discounts.RemoveRange(discounts);

                    var codes = await shopDbContext.Codes.Where(x => x.OfferId == ID).ToListAsync();
                    if(codes != null)
                        shopDbContext.Codes.RemoveRange(codes);

                    shopDbContext.Offers.Remove(checkOffer);

                    await shopDbContext.SaveChangesAsync();

                    string fileName = string.Concat("Offers/", ID);
                    DeleteFile(fileName);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }
            return Ok("Offer deleted!");
        }
    }
}
