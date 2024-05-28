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

        /// <summary>
        /// Adds new contact to the system
        /// </summary>
        /// <returns>Success message</returns>
        /// <param name="contact">New contact</param>
        /// <response code="200">Contact added successfully</response>
        /// <response code="400">Bad request</response>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("AddContact")]
        public async Task<IActionResult> AddContact([FromBody] ContactInfoModel contact)
        {
            using (var clientTransaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    DbContexts.MainContext.ContactInfo newDbContextsContact = new()
                    {
                        Name = contact.Name,
                        Email = contact.Email,
                        Phone = contact.Phone,
                        Position = contact.Position
                    }; // it would be great to have a constructor in ContactInfo
                       // that takes a ContactInfoModel object as a parameter and
                       // initializes the properties with the values from the model
                    await clientDbContext.ContactInfos.AddAsync(newDbContextsContact);
                    await clientDbContext.SaveChangesAsync();
                    await clientTransaction.CommitAsync();
                    return Ok("Contact added successfully!"); // returns 200 OK response
                }
                catch (Exception e)
                {
                    await clientTransaction.RollbackAsync();
                    return BadRequest(e.Message); // returns 400 Bad Request response
                }
            }
        }

    }
}




