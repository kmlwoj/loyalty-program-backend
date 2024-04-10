using DydaktykaBackend.Models;
using lojalBackend.DbContexts.MainContext;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? ConnStr;

        public UserController(ILogger<UserController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            ConnStr = configuration.GetConnectionString("MainConn");
        }
        /// <summary>
        /// Registers new user (with manager privileges)
        /// </summary>
        /// <param name="user">Object with user data</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Manager")]
        [HttpPost("AddUser")]
        public async Task<IActionResult> AddUser([FromBody] UserModel user)
        {
            if (!user.IsPassword())
                return BadRequest("Password was not supplied!");
            if (string.IsNullOrEmpty(user.Username))
                return BadRequest("Username was not supplied!");
            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("Email was not supplied!");
            if (user.AccountType is null)
                return BadRequest("Type of an account was not supplied!");
            if (user.AccountType.Equals(AccountTypes.Administrator))
                return BadRequest("You cannot create administrator accounts!");

            using (LojClientDbContext db = new(ConnStr))
            {
                User? dbUser = await db.Users.FindAsync(user.Username);
                if (dbUser != null)
                    return BadRequest("Username is taken!");
                string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                Organization? dbOrg = await db.Organizations.FindAsync(organization);
                if (dbOrg == null)
                    return NotFound("Given organization not found in the system!");

                byte[] salt = user.EncryptPassword();

                var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    User newUser = new()
                    {
                        Login = user.Username,
                        Password = user.Password,
                        Email = user.Email,
                        Type = user.ConvertFromEnum(),
                        Organization = organization,
                        Salt = Convert.ToHexString(salt)
                    };
                    db.Users.Add(newUser);

                    RefreshToken token = new()
                    {
                        Login = user.Username
                    };
                    db.RefreshTokens.Add(token);

                    await db.SaveChangesAsync();

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            return Ok("User successfully added!");
        }
    }
}
