using lojalBackend.DbContexts.MainContext;
using lojalBackend.DbContexts.ShopContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

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
    }
}
