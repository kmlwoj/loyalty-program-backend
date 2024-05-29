using lojalBackend.DbContexts.ShopContext;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

// using Microsoft.EntityFrameworkCore;


namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ContactController : ControllerBase
    {
        private readonly ILogger<ContactController> _logger; // what is it for? who uses it?
        private readonly IConfiguration _configuration; // what is it for? who uses it?
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext; // what is it for? who uses it?

        public ContactController(ILogger<ContactController> logger,
            IConfiguration configuration,
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
        [AllowAnonymous] // todo: what proper role and policy ?
        [HttpGet("GetContacts")]
        public async Task<IActionResult> GetContacts()
        {
            List<ContactInfoModel> answer = new();
            await foreach (var entry in clientDbContext.ContactInfos.AsAsyncEnumerable())
            {
                answer.Add(new ContactInfoModel(entry.Id, entry.Name, entry.Email,
                    entry.Phone, entry.Position));
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
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")] // todo: what proper role and policy ?
        [HttpPost("AddContact")]
        public async Task<IActionResult> AddContact([FromBody] ContactInfoModel contact)
        {
            using (var clientTransaction =
                   await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    ContactInfo newDbContextsContact = new()
                    {
                        Name = contact.Name,
                        Email = contact.Email,
                        Phone = contact.Phone,
                        Position = contact.Position
                    };
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

        /// <summary>
        /// Deletes contact from the system
        /// </summary>
        /// <param name="id">id of contact to delete</param>
        /// <returns>Sucess message</returns>
        /// <response code="200">Contact deleted successfully</response>
        /// <response code="400">Bad request</response>
        /// <response code="404">Contact not found</response>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")] // todo: what proper role and policy ?
        [HttpDelete("DeleteContact")]
        public async Task<IActionResult> DeleteContact([FromBody] int id)
        {
            var contactInfoToDelete =
                await clientDbContext.ContactInfos.FindAsync(id);

            if (contactInfoToDelete == null)
                return NotFound($"Contact with id {id} was not found in the system.");

            using (var clientTransaction =
                   await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    clientDbContext.ContactInfos.Remove(contactInfoToDelete);
                    await clientDbContext.SaveChangesAsync();
                    await clientTransaction.CommitAsync();
                    return Ok($"Contact with id {id} was found and deleted from the  system.");
                }
                catch (Exception e)
                {
                    await clientTransaction.RollbackAsync();
                    return BadRequest(e.Message);
                }
            }
        }

        /// <summary>
        /// Adds new contact request to the system
        /// </summary>
        /// <returns>Success message</returns>
        /// <response code="200">Contact request added successfully</response>
        /// <response code="400">Bad request</response>
        [AllowAnonymous]
        [HttpPost("AddContactRequest")]
        public async Task<IActionResult> AddContactRequest(
            [FromBody] ContactRequestModel contactRequest)
        {
            using (var clientTransaction =
                   await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    ContactRequest newDbContextsContactRequest = new()
                    {
                        ContReqDate = contactRequest.ContReqDate,
                        Subject = contactRequest.Subject,
                        Body = contactRequest.Body
                    };
                    await clientDbContext.ContactRequests.AddAsync(newDbContextsContactRequest);
                    await clientDbContext.SaveChangesAsync();
                    await clientTransaction.CommitAsync();
                    return Ok("Contact request added successfully!");
                }
                catch (Exception e)
                {
                    await clientTransaction.RollbackAsync();
                    return BadRequest(e.Message); // returns 400 Bad Request response
                }
            }
        }

        /// <summary>
        /// Retrieves list of every CONTACT_REQUEST in the system
        /// </summary>
        /// <returns>List with every CONTACT_REQUEST</returns>
        /// <response code="200">List of CONTACT_REQUESTs</response>
        /// <response code="400">Bad request</response>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")] // todo: what proper role and policy ?
        [HttpGet("GetAllContactRequests")]
        public async Task<IActionResult> GetAllContactRequests()
        {
            List<ContactRequestModel> answer = new();
            await foreach (var entity in clientDbContext.ContactRequests.AsAsyncEnumerable())
            {
                answer.Add(new ContactRequestModel(entity));
            }

            return new JsonResult(answer);
        }

        /// <summary>
        /// Deletes contact from the system
        /// </summary>
        /// <param name="id">id of contact request to delete</param>
        /// <returns>Sucess message</returns>
        /// <response code="200">Contact request deleted successfully</response>
        /// <response code="400">Bad request</response>
        /// <response code="404">Contact request not found</response>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")] // todo: what proper role and policy ?
        [HttpDelete("DeleteContactRequest")]
        public async Task<IActionResult> DeleteContactRequest([FromBody] int id)
        {
            var contactRequestToDelete =
                await clientDbContext.ContactRequests.FindAsync(id);

            if (contactRequestToDelete == null)
                return NotFound($"Contact request with id {id} was not found in the system.");

            using (var clientTransaction =
                   await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    clientDbContext.ContactRequests.Remove(contactRequestToDelete);
                    await clientDbContext.SaveChangesAsync();
                    await clientTransaction.CommitAsync();
                    return Ok($"Contact request with id {id} was found and deleted from the  system.");
                }
                catch (Exception e)
                {
                    await clientTransaction.RollbackAsync();
                    return BadRequest(e.Message);
                }
            }
        }

    }
}