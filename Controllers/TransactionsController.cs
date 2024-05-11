using DocumentFormat.OpenXml.Vml.Office;
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
                DbContexts.MainContext.Discount? discount = null;
                if (code != null && code.DiscId != null)
                    discount = await clientDbContext.Discounts.FindAsync(code.DiscId);

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
                DbContexts.MainContext.Discount? discount = null;
                if (code != null && code.DiscId != null)
                    discount = await clientDbContext.Discounts.FindAsync(code.DiscId);

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
        /// <summary>
        /// Buys a code from a given offer for the current user
        /// </summary>
        /// <param name="offerID">Targeted offer ID</param>
        /// <returns>Object with code information of NewCodeModel schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpPost("BuyCode/{offerID:int}")]
        public async Task<IActionResult> BuyCode(int offerID)
        {
            Claim? login = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));
            if (login == null)
                return BadRequest("User login not found in the token!");
            User? dbUser = await clientDbContext.Users.FindAsync(login.Value);
            if (dbUser == null)
                return BadRequest("User not found in the database!");
            if (!await shopDbContext.Offers.AnyAsync(x => offerID == x.OfferId))
                return NotFound("Given offer not found!");

            DbContexts.ShopContext.Code? foundCode;
            using (var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
            using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    foundCode = await shopDbContext.Codes.Where(x => offerID == x.OfferId && x.State == 1).OrderBy(x => x.Expiry).FirstOrDefaultAsync();
                    if (foundCode == null)
                    {
                        shopTransaction.Rollback();
                        clientTransaction.Rollback();
                        return BadRequest("No code is available for purchase!");
                    }

                    DbContexts.ShopContext.Offer? shopOffer = await shopDbContext.Offers.FindAsync(offerID);
                    DbContexts.ShopContext.Discount? shopDiscount = await shopDbContext.Discounts
                    .Where(x => offerID.Equals(x.OfferId) && x.Expiry.CompareTo(DateTime.UtcNow) > 0)
                    .FirstOrDefaultAsync();

                    int price = 0;
                    if(shopDiscount == null)
                    {
                        price = shopOffer == null ? 0 : shopOffer.Price;
                    }
                    else
                    {
                        DiscountModel tmpDiscount = new(shopDiscount.DiscId, shopDiscount.Name, shopDiscount.Reduction, shopOffer == null ? 0 :shopOffer.Price);
                        price = tmpDiscount.NewPrice ?? 0;
                    }

                    if (dbUser.Credits < price)
                        return BadRequest("Not enough credits to cover the transaction!");

                    if (shopOffer != null)
                    { 
                        dbUser.Credits -= price;
                        clientDbContext.Update(dbUser);

                        foundCode.State = 0;
                        shopDbContext.Update(foundCode);

                        DbContexts.MainContext.Offer newOffer = new()
                        {
                            OfferId = shopOffer.OfferId,
                            Name = shopOffer.Name,
                            Price = shopOffer.Price,
                            Organization = shopOffer.Organization,
                            Category = shopOffer.Category
                        };
                        await clientDbContext.Offers.AddAsync(newOffer);

                        DbContexts.MainContext.Discount? newDiscount = null;
                        if (shopDiscount != null)
                        {
                            newDiscount = new()
                            {
                                DiscId = shopDiscount.DiscId,
                                OfferId = shopDiscount.OfferId,
                                Name = shopDiscount.Name,
                                Reduction = shopDiscount.Reduction
                            };
                            await clientDbContext.Discounts.AddAsync((DbContexts.MainContext.Discount)newDiscount);
                        }

                        DbContexts.MainContext.Code newCode = new()
                        {
                            CodeId = foundCode.CodeId,
                            OfferId = offerID,
                            DiscId = newDiscount?.DiscId,
                            Expiry = foundCode.Expiry
                        };
                        await clientDbContext.Codes.AddAsync(newCode);

                        Transaction newTransaction = new()
                        {
                            Login = login.Value,
                            TransDate = DateTime.UtcNow,
                            CodeId = newCode.CodeId,
                            OfferId = newCode.OfferId,
                            Price = price,
                            Shop = newOffer.Organization
                        };
                        await clientDbContext.Transactions.AddAsync(newTransaction);

                        await clientDbContext.SaveChangesAsync();
                        await shopDbContext.SaveChangesAsync();
                    }
                }
                catch
                {
                    shopTransaction.Rollback();
                    clientTransaction.Rollback();
                    throw;
                }
                await shopTransaction.CommitAsync();
                await clientTransaction.CommitAsync();
            }

            return new JsonResult(new NewCodeModel(foundCode.CodeId, foundCode.Expiry));
        }
    }
}
