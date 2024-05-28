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
        /// Retrieves information about available code count
        /// </summary>
        /// <param name="offerID">Targeted offer ID</param>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("CheckAvailableCodes/{offerID:int}")]
        public async Task<IActionResult> CheckAvailableCodes(int offerID)
        {
            if (!await shopDbContext.Offers.AnyAsync(x => offerID == x.OfferId))
                return NotFound("Given offer not found!");

            return new JsonResult(await shopDbContext.Codes.Where(x => offerID == x.OfferId && DateTime.UtcNow.CompareTo(x.Expiry) < 0 && x.State == 1).CountAsync());
        }
        /// <summary>
        /// Buys a code from a given offer for the current user
        /// </summary>
        /// <param name="offerID">Targeted offer ID</param>
        /// <param name="amount">Amount of codes to buy (default is 1)</param>
        /// <returns>Object with code information of NewCodeModel schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpPost("BuyCode/{offerID:int}")]
        public async Task<IActionResult> BuyCode(int offerID, [FromQuery] int amount = 1)
        {
            if(amount < 1)
            {
                return BadRequest("Amount must be higher than 1!");
            }
            List<NewCodeModel> answer = new();

            Claim? login = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));
            if (login == null)
                return BadRequest("User login not found in the token!");
            User? dbUser = await clientDbContext.Users.FindAsync(login.Value);
            if (dbUser == null)
                return BadRequest("User not found in the database!");
            if (!await shopDbContext.Offers.AnyAsync(x => offerID == x.OfferId))
                return NotFound("Given offer not found!");

            using (var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
            using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var shopCodes = await shopDbContext.Codes.Where(x => offerID == x.OfferId && DateTime.UtcNow.CompareTo(x.Expiry) < 0 && x.State == 1).OrderBy(x => x.Expiry).ToListAsync();
                    if(shopCodes.Count < amount)
                    {
                        shopTransaction.Rollback();
                        clientTransaction.Rollback();
                        return BadRequest("There are less codes in the system than ordered!");
                    }
                    shopCodes = shopCodes.Take(amount).ToList();

                    DbContexts.ShopContext.Offer? shopOffer = await shopDbContext.Offers.FindAsync(offerID);
                    DbContexts.ShopContext.Discount? shopDiscount = await shopDbContext.Discounts
                    .Where(x => offerID.Equals(x.OfferId) && x.Expiry.CompareTo(DateTime.UtcNow) > 0)
                    .FirstOrDefaultAsync();

                    int price = 0;
                    if(shopDiscount == null)
                    {
                        price = shopOffer == null ? 0 : shopOffer.Price * shopCodes.Count;
                    }
                    else
                    {
                        DiscountModel tmpDiscount = new(shopDiscount.DiscId, shopDiscount.Name, shopDiscount.Reduction, shopOffer == null ? 0 :shopOffer.Price);
                        price = tmpDiscount.NewPrice == null ? 0 : (int)tmpDiscount.NewPrice * shopCodes.Count;
                    }

                    if (dbUser.Credits < price)
                        return BadRequest("Not enough credits to cover the transaction!");

                    if (shopOffer != null)
                    { 
                        dbUser.Credits -= price;
                        clientDbContext.Update(dbUser);

                        foreach(var code in shopCodes)
                        {
                            code.State = 0;
                            shopDbContext.Update(code);
                        }
                        if(await clientDbContext.Offers.FindAsync(shopOffer.OfferId) == null)
                        {
                            DbContexts.MainContext.Offer newOffer = new()
                            {
                                OfferId = shopOffer.OfferId,
                                Name = shopOffer.Name,
                                Price = shopOffer.Price,
                                Organization = shopOffer.Organization,
                                Category = shopOffer.Category
                            };
                            await clientDbContext.Offers.AddAsync(newOffer);
                        }
                        
                        DbContexts.MainContext.Discount? newDiscount = null;
                        if (shopDiscount != null && await clientDbContext.Discounts.FindAsync(shopDiscount.DiscId) == null)
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
                        foreach(var code in shopCodes)
                        {
                            DbContexts.MainContext.Code newCode = new()
                            {
                                CodeId = code.CodeId,
                                OfferId = offerID,
                                DiscId = newDiscount?.DiscId,
                                Expiry = code.Expiry
                            };
                            await clientDbContext.Codes.AddAsync(newCode);                        

                            Transaction newTransaction = new()
                            {
                                Login = login.Value,
                                TransDate = DateTime.UtcNow,
                                CodeId = newCode.CodeId,
                                OfferId = newCode.OfferId,
                                Price = price,
                                Shop = shopOffer.Organization
                            };
                            await clientDbContext.Transactions.AddAsync(newTransaction);

                            answer.Add(new(code.CodeId, code.Expiry));
                        }

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

            return new JsonResult(answer);
        }
    }
}
