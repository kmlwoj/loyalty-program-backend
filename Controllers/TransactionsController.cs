using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using static lojalBackend.ImageManager;

namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly ILogger<TransactionsController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public TransactionsController(ILogger<TransactionsController> logger, IConfiguration configuration, LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }
        /// <summary>
        /// Retrieves every transaction done by the current user
        /// </summary>
        /// <returns>List of every transaction of TransactionModel schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetTransactions")]
        public async Task<IActionResult> GetTransactions()
        {
            List<TransactionModel> answer = new();
            Claim? login = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));

            if (login == null)
                return Unauthorized("User does not have name identifier claim!");

            foreach (var entry in await clientDbContext.Transactions.Where(x => x.Login.Equals(login.Value)).OrderByDescending(x => x.TransDate).ToListAsync())
            {
                DbContexts.MainContext.Code? code = await clientDbContext.Codes.FindAsync(entry.CodeId, entry.OfferId);
                DbContexts.MainContext.Offer? offer = await clientDbContext.Offers.FindAsync(entry.OfferId);
                DbContexts.MainContext.Discount? discount = await clientDbContext.Discounts.Where(x => x.OfferId == entry.OfferId).FirstOrDefaultAsync();

                if (code != null && offer != null) {
                    string fileName = string.Concat("Offers/", entry.OfferId);
                    answer.Add(new(
                        entry.TransId,
                        new(
                            offer.OfferId,
                            offer.Name,
                            offer.Price,
                            offer.Organization,
                            offer.Category,
                            discount != null ? new(
                                discount.DiscId,
                                discount.Name,
                                discount.Reduction,
                                offer.Price
                                ) : null,
                            CheckFileExistence(fileName)
                            ),
                        new(
                            code.CodeId,
                            code.Expiry
                            ),
                        entry.TransDate
                        ));
                }
            }

            return new JsonResult(answer);
        }
        /// <summary>
        /// Retrieves every transaction in the system
        /// </summary>
        /// <param name="organization">Optional parameter that specifies which shop will be included in the list</param>
        /// <returns>List of every transaction of TransactionModel schema</returns>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpGet("GetAllTransactions")]
        public async Task<IActionResult> GetAllTransactions([FromQuery] string? organization)
        {
            List<TransactionModel> answer = new();

            List<Transaction> forEachList;
            if(organization != null)
            {
                if(!"Shop".Equals((await clientDbContext.Organizations.FindAsync(organization))?.Type))
                {
                    return BadRequest("Given organization is not a shop!");
                }
                forEachList = await clientDbContext.Transactions.Where(x => x.Shop.Equals(organization)).OrderByDescending(x => x.TransDate).ToListAsync();
            }
            else
            {
                forEachList = await clientDbContext.Transactions.OrderByDescending(x => x.TransDate).ToListAsync();
            }
            foreach (var entry in forEachList)
            {
                DbContexts.MainContext.Code? code = await clientDbContext.Codes.FindAsync(entry.CodeId, entry.OfferId);
                DbContexts.MainContext.Offer? offer = await clientDbContext.Offers.FindAsync(entry.OfferId);
                DbContexts.MainContext.Discount? discount = await clientDbContext.Discounts.Where(x => x.OfferId == entry.OfferId).FirstOrDefaultAsync();

                if (code != null && offer != null)
                {
                    string fileName = string.Concat("Offers/", entry.OfferId);
                    answer.Add(new(
                        entry.TransId,
                        new(
                            offer.OfferId,
                            offer.Name,
                            offer.Price,
                            offer.Organization,
                            offer.Category,
                            discount != null ? new(
                                discount.DiscId,
                                discount.Name,
                                discount.Reduction,
                                offer.Price
                                ) : null,
                            CheckFileExistence(fileName)
                            ),
                        new(
                            code.CodeId,
                            code.Expiry
                            ),
                        entry.TransDate
                        ));
                }
            }

            return new JsonResult(answer);
        }
        /// <summary>
        /// Retrieves information about code availability in a given offer
        /// </summary>
        /// <param name="offerID">Targeted offer ID</param>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("IsCodeAvailable/{offerID:int}")]
        public async Task<IActionResult> IsCodeAvailable(int offerID)
        {
            if (!await shopDbContext.Offers.AnyAsync(x => offerID == x.OfferId))
                return NotFound("Given offer not found!");

            return new JsonResult(await shopDbContext.Codes.AnyAsync(x => offerID == x.OfferId && x.State == 1));
        }
    }
}
