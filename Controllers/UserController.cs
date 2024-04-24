using DydaktykaBackend.Models;
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
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly IConfiguration _configuration;
        private readonly LojClientDbContext clientDbContext;
        private readonly LojShopDbContext shopDbContext;

        public UserController(ILogger<UserController> logger, IConfiguration configuration, LojClientDbContext clientDbContext, LojShopDbContext shopDbContext)
        {
            _logger = logger;
            _configuration = configuration;
            this.clientDbContext = clientDbContext;
            this.shopDbContext = shopDbContext;
        }
        /// <summary>
        /// Retrieves information about the current user
        /// </summary>
        /// <returns>Current user data of UserDbModelOrg schema</returns>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("GetCurrentUser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            string? user = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("sub"))?.Value;
            User? dbUser = await clientDbContext.Users.FindAsync(user);
            UserDbModelOrg? answer = null;
            if (user != null && dbUser != null)
            {
                answer = new(
                    user,
                    dbUser.Email,
                    UserModel.ConvertToEnum(dbUser.Type),
                    dbUser.Credits ?? 0,
                    dbUser.LatestUpdate,
                    dbUser.Organization
                    );
            }
            return answer != null ? new JsonResult(answer) : NotFound("Credentials of the current user not found!");
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

            User? dbUser = await clientDbContext.Users.FindAsync(user.Username);
            if (dbUser != null)
                return BadRequest("Username is taken!");
            string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
            DbContexts.MainContext.Organization? dbOrg = await clientDbContext.Organizations.FindAsync(organization);
            if (dbOrg == null)
                return NotFound("Given organization not found in the system!");

            byte[] salt = user.EncryptPassword();

            using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
            {
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
                    clientDbContext.Users.Add(newUser);

                    RefreshToken token = new()
                    {
                        Login = user.Username
                    };
                    clientDbContext.RefreshTokens.Add(token);

                    await clientDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
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

            DbContexts.MainContext.Organization? dbOrg = await clientDbContext.Organizations.FindAsync(localOrganization);
            if (dbOrg == null)
                return NotFound("Given organization not found in the system!");

            var dbUsers = clientDbContext.Users.Where(x => x.Organization.Equals(localOrganization));
            foreach (var user in dbUsers)
            {
                users.Add(new(user.Login, user.Email, UserModel.ConvertToEnum(user.Type), user.Credits ?? 0, user.LatestUpdate));
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
            
            User? editedUser = await clientDbContext.Users.FindAsync(user.Login);

            if (editedUser == null)
                return NotFound("User with the given login does not exist!");

            if (HttpContext.User.IsInRole("Manager"))
            {
                string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                if (organization == null)
                    return BadRequest("Organization not given in the user credentials!");
                if (!organization.Equals(editedUser?.Organization))
                    return BadRequest("Manager cannot change data of users from another organization!");
            }

            using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    editedUser.Email = user.Email;
                    editedUser.LatestUpdate = DateTime.UtcNow;
                    clientDbContext.Update(editedUser);
                    await clientDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
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
            User? tmpUser = await clientDbContext.Users.FindAsync(user.Login);

            if (tmpUser == null)
                return NotFound("User with the given login does not exist!");

            if (HttpContext.User.IsInRole("Manager"))
            {
                string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                if (organization == null)
                    return BadRequest("Organization not given in the user credentials!");
                if (!organization.Equals(tmpUser?.Organization))
                    return BadRequest("Manager cannot delete user from another organization!");
            }

            using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    RefreshToken? token = await clientDbContext.RefreshTokens.FindAsync(tmpUser.Login);
                    if (token != null)
                        clientDbContext.RefreshTokens.Remove(token);

                    var transactions = clientDbContext.Transactions.Where(x => tmpUser.Login.Equals(x.Login));
                    if (transactions != null)
                        clientDbContext.Transactions.RemoveRange(transactions);

                    clientDbContext.Users.Remove(tmpUser);

                    await clientDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok("User deleted!");
        }
        /// <summary>
        /// Changes targeted user's credits
        /// </summary>
        /// <param name="login">Targeted user</param>
        /// <param name="amount">Desired amount for credit change</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Manager,Administrator")]
        [HttpPost("ChangeCredits/{amount:int}")]
        public async Task<IActionResult> ChangeCredits([FromBody] string login, int amount)
        {
            User? dbUser = await clientDbContext.Users.FindAsync(login);

            if (dbUser == null)
                return NotFound("User with the given login was not found in the system!");

            if (HttpContext.User.IsInRole("Manager"))
            {
                string? organization = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("azp"))?.Value;
                if (organization == null)
                    return BadRequest("Organization not given in the user credentials!");
                if (!organization.Equals(dbUser?.Organization))
                    return BadRequest("Manager cannot manage user from another organization!");
            }

            using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    if (amount < 0)
                    {
                        if (dbUser.Credits <= Math.Abs(amount))
                            dbUser.Credits = 0;
                    }
                    else
                    {
                        dbUser.Credits ??= 0;
                        dbUser.Credits += amount;
                    }
                    dbUser.LatestUpdate = DateTime.UtcNow;
                    clientDbContext.Update(dbUser);
                    await clientDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok("Credits changed for the targeted user!");
        }
        /// <summary>
        /// Changes password of currently logged in user
        /// </summary>
        /// <param name="password">Desired new password</param>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpPut("SetPassword")]
        public async Task<IActionResult> SetPassword([FromBody] string password)
        {
            if (string.IsNullOrEmpty(password))
                return BadRequest("Password cannot be empty!");

            string? username = HttpContext?.User?.Claims?.FirstOrDefault(c => c.Type.Contains("sub"))?.Value;
            if (string.IsNullOrEmpty(username))
                return BadRequest("Username in auth token is empty!");

            User? dbUser = await clientDbContext.Users.FindAsync(username);
            LoginModel tmpUser = new(username, password);
            string salt = string.Empty;
            AdminUserModel? userModel = null;
            if (dbUser != null)
            {
                userModel = new(
                                dbUser.Login,
                                dbUser.Password,
                                UserModel.ConvertToEnum(dbUser.Type),
                                dbUser.Email,
                                dbUser.Organization
                            );
                salt = dbUser.Salt;
            }
            if (userModel != null && userModel.VerifyPassword(tmpUser.GetSecurePassword(), salt))
                return BadRequest("New password cannot be the same as the old one!");

            using (var transaction = await clientDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    UserModel newPasswordUser = new(username, password);
                    byte[] newSalt = newPasswordUser.EncryptPassword();
                    if (dbUser != null)
                    {
                        dbUser.Password = newPasswordUser.Password;
                        dbUser.Salt = Convert.ToHexString(newSalt);
                        dbUser.LatestUpdate = DateTime.UtcNow;
                        clientDbContext.Update(dbUser);
                    }
                    await clientDbContext.SaveChangesAsync();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
                await transaction.CommitAsync();
            }

            return Ok("Password changed!");
        }
    }
}
