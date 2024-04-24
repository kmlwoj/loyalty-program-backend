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
    public class CategoriesController : ControllerBase
    {
        private readonly ILogger<CategoriesController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public CategoriesController(ILogger<CategoriesController> logger, IConfiguration configuration, LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }
        /// <summary>
        /// Retrieves list of every category in the system
        /// </summary>
        /// <returns>List with every category</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetCategories")]
        public async Task<IActionResult> GetCategories()
        {
            List<CategoryModel> answer = new();
            await foreach(var entry in shopDbContext.Categories.AsAsyncEnumerable())
            {
                answer.Add(new(entry.Name));
            }
            return new JsonResult(answer);
        }
        /// <summary>
        /// Adds new offer category to the system
        /// </summary>
        /// <param name="category">New category</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("AddCategory")]
        public async Task<IActionResult> AddCategory([FromBody] string category)
        {
            DbContexts.ShopContext.Category? checkCat = await shopDbContext.Categories.FindAsync(category);
            if (checkCat != null)
                return BadRequest("Category already exists in the system!");

            using (var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
            using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    DbContexts.MainContext.Category clientCategory = new()
                    { 
                        Name = category 
                    };
                    clientDbContext.Categories.Add(clientCategory);
                    DbContexts.ShopContext.Category shopCategory = new()
                    {
                        Name = category
                    };
                    shopDbContext.Categories.Add(shopCategory);
                    
                    await clientDbContext.SaveChangesAsync();
                    await shopDbContext.SaveChangesAsync();
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

            return Ok("Added category!");
        }
        /// <summary>
        /// Deletes a given category from the system
        /// </summary>
        /// <param name="category">Targeted category</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpDelete("DeleteCategory")]
        public async Task<IActionResult> DeleteCategory([FromBody] string category)
        {
            DbContexts.ShopContext.Category? checkCat = await shopDbContext.Categories.FindAsync(category);
            if (checkCat == null)
                return NotFound("Category was not found in the system!");

            using (var shopTransaction = await shopDbContext.Database.BeginTransactionAsync())
            using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    var shopOffers = await shopDbContext.Offers.Where(x => category.Equals(x.Category)).ToListAsync();
                    foreach (var offer in shopOffers)
                    {
                        offer.Category = null;
                    }
                    var clientOffers = await clientDbContext.Offers.Where(x => category.Equals(x.Category)).ToListAsync();
                    foreach (var offer in clientOffers)
                    {
                        offer.Category = null;
                    }
                    DbContexts.MainContext.Category? clientCat = await clientDbContext.Categories.FindAsync(category);
                    if(clientCat != null)
                        clientDbContext.Categories.Remove(clientCat);
                    shopDbContext.Categories.Remove(checkCat);

                    await clientDbContext.SaveChangesAsync();
                    await shopDbContext.SaveChangesAsync();
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

            return Ok("Deleted category!");
        }
    }
}
