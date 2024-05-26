using lojalBackend.DbContexts.ShopContext;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public ContactController(ILogger<ContactController> logger, IConfiguration configuration,
            LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }

        /// <summary>
        /// Retrieves list of every CONTACT_INFO in the system
        /// </summary>
        /// <returns>List with every CONTACT_INFO</returns>
        [AllowAnonymous]
        [HttpGet("GetContacts")]
        public async Task<IActionResult> GetContacts()
        {
            List<ContactInfoModel> answer = new();
            await foreach(var entry in clientDbContext.ContactInfos.AsAsyncEnumerable())
            {
                answer.Add(new ContactInfoModel(entry.Name, entry.Email, entry.Phone, entry.Position));
            }
            return new JsonResult(answer);
        }



    }
}




