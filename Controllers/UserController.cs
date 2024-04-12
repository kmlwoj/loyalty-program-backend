using DydaktykaBackend.Models;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Models;
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
                        Organization = organization ?? string.Empty,
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
        /// <summary>
        /// Retrieves list of users from a given organization
        /// </summary>
        /// <param name="organization">Targeted organization only for administration (null will get the user's organization)</param>
        /// <returns>Object of UserDbModel type</returns>
        [Authorize(Policy = "IsLoggedIn", Roles = "Manager,Administrator")]
        [HttpGet("GetUsers")]
        public async Task<IActionResult> GetUsers([FromQuery] string? organization)
        {
            if (organization is not null && HttpContext.User.IsInRole("Manager"))
                return BadRequest("Manager cannot check users of different organizations!");

            List<UserDbModel> users = new();
            string? localOrganization = organization is null ? HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value : organization;

            using (LojClientDbContext db = new(ConnStr))
            {
                Organization? dbOrg = await db.Organizations.FindAsync(localOrganization);
                if (dbOrg == null)
                    return NotFound("Given organization not found in the system!");

                var dbUsers = db.Users.Where(x => x.Organization.Equals(localOrganization));
                foreach (var user in dbUsers)
                {
                    users.Add(new(user.Login, user.Email, UserModel.ConvertToEnum(user.Type), user.Credits ?? 0, user.LatestUpdate));
                }
            }
            return new JsonResult(users);
        }
        /// <summary>
        /// Edits email of a given user
        /// </summary>
        /// <param name="user">Object of UserDbModel type with new mail</param>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpPut("EditUserMail")]
        public async Task<IActionResult> EditUserMail([FromBody] UserDbModel user)
        {
            if(HttpContext.User.IsInRole("Worker"))
            {
                string? username = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("sub"))?.Value;
                if (!user.Login.Equals(username))
                    return BadRequest("Worker cannot change data of another user!");
            }
            using (LojClientDbContext db = new(ConnStr))
            {
                User? editedUser = await db.Users.FindAsync(user.Login);
                if (HttpContext.User.IsInRole("Manager"))
                {
                    string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                    if (organization == null)
                        return BadRequest("Organization not given in the user credentials!");
                    if (!organization.Equals(editedUser?.Organization))
                        return BadRequest("Manager cannot change data of users from another organization!");
                }

                if (editedUser == null)
                    return NotFound("User with the given login does not exist!");

                var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    editedUser.Email = user.Email;
                    editedUser.LatestUpdate = DateTime.UtcNow;
                    db.Update(editedUser);
                    await db.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                transaction.Commit();
            }
            return Ok("User mail updated!");
        }
        /// <summary>
        /// Deletes a given user
        /// </summary>
        /// <param name="user">Data of user</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Manager,Administrator")]
        [HttpDelete("DeleteUser")]
        public async Task<IActionResult> DeleteUser([FromBody] UserDbModel user)
        {
            using (LojClientDbContext db = new(ConnStr))
            {
                User? tmpUser = await db.Users.FindAsync(user.Login);
                if (HttpContext.User.IsInRole("Manager"))
                {
                    string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                    if (organization == null)
                        return BadRequest("Organization not given in the user credentials!");
                    if (!organization.Equals(tmpUser?.Organization))
                        return BadRequest("Manager cannot delete user from another organization!");
                }

                if (tmpUser == null)
                    return NotFound("User with the given login does not exist!");

                var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    db.Users.Remove(tmpUser);
                    await db.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                transaction.Commit();
            }
            return Ok("User deleted!");
        }
    }
}
