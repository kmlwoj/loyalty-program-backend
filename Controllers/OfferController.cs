using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
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
        /// <returns>List of offers of ShopOfferModel schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetOffers/{organization}")]
        public async Task<IActionResult> GetOffers(string organization)
        {
            DbContexts.MainContext.Organization? checkOrg = await clientDbContext.Organizations.FindAsync(organization);
            if (checkOrg == null)
                return NotFound("Requested organization was not found in the system!");
            if (!checkOrg.Type.Equals(Enum.GetName(typeof(OrgTypes), OrgTypes.Shop)))
                return BadRequest("Organization is not a registered shop!");

            List<ShopOfferModel> answer = new();
            foreach (var entry in await shopDbContext.Offers.Where(x => organization.Equals(x.Organization)).ToListAsync())
            {
                DbContexts.ShopContext.Discount? discount = await shopDbContext.Discounts
                    .Where(x => entry.OfferId.Equals(x.OfferId) && x.Expiry.CompareTo(DateTime.UtcNow) > 0)
                    .FirstOrDefaultAsync();

                string fileName = string.Concat("Offers/", entry.OfferId);
                answer.Add(new(
                    entry.OfferId,
                    entry.Name,
                    entry.Price,
                    organization,
                    entry.State > 0, //mysql ef bug
                    entry.Category,
                    discount != null ? new(
                        discount.DiscId,
                        discount.Name,
                        discount.Reduction,
                        discount.Expiry,
                        entry.Price
                        ) : null,
                    CheckFileExistence(fileName)
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
        public async Task<IActionResult> AddOffer([FromBody] ShopOfferModel offer)
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
        public async Task<IActionResult> ChangeOffer([FromBody] ShopOfferModel offer)
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
        /// <summary>
        /// Sets offer discount details
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <param name="discount">Object with discount details (null means clear discount)</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPut("SetOfferDiscount/{ID:int}")]
        public async Task<IActionResult> SetOfferDiscount(int ID, [FromBody] ShopDiscountModel? discount)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    DateTime now = DateTime.UtcNow;
                    DbContexts.ShopContext.Discount? checkDiscount = await shopDbContext.Discounts.Where(x => x.OfferId.Equals(ID) && x.Expiry.CompareTo(now) > 0).FirstOrDefaultAsync();
                    if(checkDiscount != null)
                    {
                        checkDiscount.Expiry = now;
                        shopDbContext.Update(checkDiscount);
                    }
                    if (discount != null)
                    {
                        if (discount.Expiry.CompareTo(now) < 0)
                        {
                            transaction.Rollback();
                            return BadRequest("Expiry of the discount is set to the past date!");
                        }
                        discount.CalculatePrice(checkOffer.Price);
                        DbContexts.ShopContext.Discount newDiscount = new()
                        {
                            OfferId = ID,
                            Name = discount.Name,
                            Reduction = discount.GetReductionString(),
                            Expiry = discount.Expiry
                        };
                        await shopDbContext.Discounts.AddAsync(newDiscount);
                    }
                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }
            return Ok(discount != null 
                ? string.Concat("Discount set for offer with ID ", ID, "!") 
                : string.Concat("Discount cleared for offer with ID ", ID, "!"));
        }
        /// <summary>
        /// Retrieves offer image with the given ID
        /// </summary>
        /// <param name="ID">Offer ID</param>
        /// <returns>Image with the content-type of image/{type}</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetOfferImage/{ID:int}")]
        public async Task<IActionResult> GetOfferImage(int ID)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            try
            {
                string fileName = string.Concat("Offers/", ID);
                FileStream answer = GetFile(fileName);
                return File(answer, $"image/{Path.GetExtension(answer.Name)[1..]}");
            }
            catch (Exception ex)
            {
                if ("Image does not exist!".Equals(ex.Message))
                    return NotFound(ex.Message);
                throw;
            }
        }
        /// <summary>
        /// Saves image for a given offer
        /// </summary>
        /// <param name="file">Form file with the image</param>
        /// <param name="ID">Targeted offer</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPut("SetOfferImage/{ID:int}")]
        public async Task<IActionResult> SetOfferImage(IFormFile file, int ID)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            if (!CheckFileExtension(file))
                return BadRequest("Image is not a jpeg or png!");

            if (!CheckFileSize(file))
                return BadRequest("File is bigger than 4MB!");

            string fileName = string.Concat("Offers/", ID);
            DeleteFile(fileName);
            await SaveFile(file, fileName);
            return Ok(string.Concat("Image added for offer with ID ", ID, "!"));
        }
        /// <summary>
        /// Deletes image from a given offer
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpDelete("DeleteOfferImage/{ID:int}")]
        public async Task<IActionResult> DeleteOfferImage(int ID)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            string fileName = string.Concat("Offers/", ID);
            if (DeleteFile(fileName))
            {
                return Ok(string.Concat("Image deleted for offer with ID ", ID, "!"));
            }
            else
            {
                return BadRequest("No image found!");
            }
        }
        /// <summary>
        /// Retrieves list of every code assigned to an offer
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <returns>List of objects of CodeModel schema</returns>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpGet("CheckOfferCodes/{ID:int}")]
        public async Task<IActionResult> CheckOfferCodes(int ID)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            List<CodeModel> answer = new();
            foreach(var code in await shopDbContext.Codes.Where(x => x.OfferId == ID).ToListAsync())
            {
                answer.Add(new(
                    code.CodeId,
                    code.State == 0,
                    code.Expiry
                    ));
            }
            return new JsonResult(answer);
        }
        /// <summary>
        /// Adds codes to a specified offer
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <param name="codes">Array of codes of NewCodeModel schema</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("AddCodes/{ID:int}")]
        public async Task<IActionResult> AddCodes(int ID, [FromBody] NewCodeModel[] codes)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");
            
            if (codes.Length == 0)
                return BadRequest("No codes given in the endpoint!");

            List<DbContexts.ShopContext.Code> tmpDbCodes = new();
            foreach (var code in codes)
            {
                tmpDbCodes.Add(new()
                {
                    CodeId = code.Code,
                    OfferId = ID,
                    State = 1,
                    Expiry = code.Expiry
                });
            }

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await shopDbContext.Codes.AddRangeAsync(tmpDbCodes);
                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok(string.Concat("Codes added to the offer with ID ", ID, "!"));
        }
        /// <summary>
        /// Adds codes from a file to a specified offer
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <param name="fileCodes">File with codes formatted in JSON with NewCodeModel schema</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("AddCodesFromFile/{ID:int}")]
        public async Task<IActionResult> AddCodesFromFile(int ID, IFormFile fileCodes)
        {
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");

            NewCodeModel[]? codes;
            if (fileCodes == null)
                return BadRequest("No codes specified in the endpoint body!");

            using (var sr = fileCodes.OpenReadStream())
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                codes = await JsonSerializer.DeserializeAsync<NewCodeModel[]>(sr, options);
            }
            if (codes == null)
                return BadRequest("No codes given in the file!");

            List<DbContexts.ShopContext.Code> tmpDbCodes = new();
            foreach (var code in codes)
            {
                tmpDbCodes.Add(new()
                {
                    CodeId = code.Code,
                    OfferId = ID,
                    State = 1,
                    Expiry = code.Expiry
                });
            }

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    await shopDbContext.Codes.AddRangeAsync(tmpDbCodes);
                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok(string.Concat("Codes added to the offer with ID ", ID, "!"));
        }
        /// <summary>
        /// Changes current state of a given code
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <param name="code">Targeted code</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPut("ChangeCodeState/{ID:int}")]
        public async Task<IActionResult> ChangeCodeState(int ID, [FromBody] int? code)
        {
            if (code == null)
                return BadRequest("No code specified in the request body!");
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");
            DbContexts.ShopContext.Code? checkCode = await shopDbContext.Codes.FindAsync(code, ID);
            if (checkCode == null)
                return NotFound("Given code was not found!");

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    checkCode.State = (ulong)(checkCode.State > 0 ? 0 : 1);
                    shopDbContext.Update(checkCode);
                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok(string.Concat("Changed state of code from offer with ID ", ID, "!"));
        }
        /// <summary>
        /// Removes a given code that is not already redeemed
        /// </summary>
        /// <param name="ID">Targeted offer ID</param>
        /// <param name="code">Targeted code</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpDelete("DeleteCode/{ID:int}")]
        public async Task<IActionResult> DeleteCode(int ID, [FromBody] int? code)
        {
            if (code == null)
                return BadRequest("No code specified in the request body!");
            DbContexts.ShopContext.Offer? checkOffer = await shopDbContext.Offers.FindAsync(ID);
            if (checkOffer == null)
                return NotFound("Offer with the given ID was not found!");
            DbContexts.ShopContext.Code? checkCode = await shopDbContext.Codes.FindAsync(code, ID);
            if (checkCode == null)
                return NotFound("Given code was not found!");
            if (checkCode.State == 0)
                return BadRequest("Code is already redeemed!");

            using (var transaction = await shopDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    shopDbContext.Remove(checkCode);
                    await shopDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok("Code deleted!");
        }
    }
}
