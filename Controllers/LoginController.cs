using DydaktykaBackend.Models;
using lojalBackend.DbContexts.MainContext;
using lojalBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace lojalBackend.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoginController : ControllerBase
    {
        private readonly ILogger<LoginController> _logger;
        private readonly IConfiguration _configuration;
        private readonly string? ConnStr;

        public LoginController(ILogger<LoginController> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            ConnStr = configuration.GetConnectionString("MainConn");
        }
        /// <summary>
        /// Logs in a given user
        /// </summary>
        /// <param name="user">Object with user login and password</param>
        [AllowAnonymous]
        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel user)
        {
            IActionResult response = Unauthorized();
            var authenticatedUser = await AuthenticateUser(user);

            if (authenticatedUser != null)
            {
                var tokenString = GenerateJSONWebToken(authenticatedUser);
                var refreshToken = GenerateRefreshToken();

                using (LojClientDbContext db = new(ConnStr))
                {
                    var transaction = await db.Database.BeginTransactionAsync();
                    try
                    {
                        RefreshToken? entry = await db.RefreshTokens.FirstOrDefaultAsync(x => user.Username.Equals(x.Login));
                        if(entry != null)
                        {
                            entry.Token = refreshToken;
                            entry.Expiry = DateTime.Now.AddDays(1);
                            db.Update(entry);
                            await db.SaveChangesAsync();
                        }
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                    transaction.Commit();
                }

                string[] keys = { "X-Access-Token", "X-Username", "X-Refresh-Token" };
                foreach(var cookie in HttpContext.Request.Cookies.Where(x => keys.Contains(x.Key)))
                {
                    Response.Cookies.Delete(cookie.Key);
                }

                Response.Cookies.Append("X-Access-Token", tokenString, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });
                Response.Cookies.Append("X-Username", user.Username, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });
                Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions() { HttpOnly = true, SameSite = SameSiteMode.Strict });
                response = Ok();
            }

            return response;
        }
        /// <summary>
        /// Registers new user (with administrator privileges)
        /// </summary>
        /// <param name="user">Object with user data</param>
        [Authorize(Policy = "IsLoggedIn", Roles = "Administrator")]
        [HttpPost("RegisterForAdmin")]
        public async Task<IActionResult> RegisterForAdmin([FromBody] AdminUserModel user)
        {
            if (!user.IsPassword())
                return BadRequest("Password was not supplied!");
            if (string.IsNullOrEmpty(user.Username))
                return BadRequest("Username was not supplied!");
            if (user.AccountType is null)
                return BadRequest("Type of an account was not supplied!");
            if (string.IsNullOrEmpty(user.Email))
                return BadRequest("Email was not supplied");

            using (LojClientDbContext db = new(ConnStr))
            {
                User? dbUser = await db.Users.FindAsync(user.Username);
                if (dbUser != null)
                    return BadRequest("Username is taken!");
                Organization? dbOrg = await db.Organizations.FindAsync(user.OrganizationName);
                if (dbOrg == null)
                    return NotFound("Given organization not found in the system!");
                else if (!dbOrg.Type.Equals("Client"))
                    return BadRequest("You can only create accounts for clients!");

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
                        Organization = user.OrganizationName,
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
        /// Logs out the current user
        /// </summary>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("Logout")]
        public async Task<IActionResult> Logout()
        {
            using (LojClientDbContext db = new(ConnStr))
            {
                Claim? login = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));

                if (login == null)
                    return Unauthorized("User does not have name identifier claim!");

                var transaction = await db.Database.BeginTransactionAsync();
                try
                {
                    RefreshToken? token = await db.RefreshTokens.FirstOrDefaultAsync(x => login.Value.Equals(x.Login));
                    if (token != null)
                    {
                        token.Token = null;
                        token.Expiry = null;
                        db.Update(token);
                        await db.SaveChangesAsync();
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            foreach (var cookie in HttpContext.Request.Cookies)
            {
                Response.Cookies.Delete(cookie.Key);
            }
            return Ok("Account logged out.");
        }
        /// <summary>
        /// Tests if the user is authorized
        /// </summary>
        [Authorize(Policy = "IsLoggedIn")]
        [HttpGet("IsLoggedIn")]
        public IActionResult IsLoggedIn()
        {
            Claim? login = HttpContext.User.Claims.FirstOrDefault(c => c.Type.Contains("sub"));

            if (login == null)
                return Unauthorized("User does not have name identifier claim!");

            return Ok($"The user is logged in.\n{login.Value}");
        }
        public static string RefreshToken(TokenModel tokens, string JWTKey, string ConnStr, string JWTIssuer, string JWTAudience)
        {
            if (tokens == null)
                throw new Exception("Invalid request! No tokens were found.");

            var principal = GetPrincipalFromExpiredToken(tokens.AccessToken, JWTKey);
            if (principal == null)
            {
                throw new Exception("Invalid access token or refresh token!");
            }

            string? username = principal.Claims.FirstOrDefault(c => c.Type.Contains("sub"))?.Value;

            if (username == null)
                throw new Exception("User not found in the token!");

            string? dbToken = null;
            DateTime? expiry = null;
            using (LojClientDbContext db = new(ConnStr))
            {
                RefreshToken? token = db.RefreshTokens.FirstOrDefault(x => username.Equals(x.Login));
                if(token != null)
                {
                    dbToken = token.Token;
                    expiry = token.Expiry;
                }
            }

            if (tokens.RefreshToken != dbToken)
                throw new Exception("Invalid access token or refresh token");
            if (expiry <= DateTime.Now)
                throw new Exception("Refresh token is expired");

            var newAccessToken = GenerateJSONWebToken(principal.Claims.ToList(), JWTKey, JWTIssuer, JWTAudience);

            return newAccessToken;
        }
        private string GenerateJSONWebToken(AdminUserModel userInfo)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? string.Empty));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[] {
                new Claim(JwtRegisteredClaimNames.Sub, userInfo.Username),
                new Claim(ClaimTypes.Role, userInfo.ConvertFromEnum()),
                new Claim(JwtRegisteredClaimNames.Email, userInfo.Email ?? ""),
                new Claim(JwtRegisteredClaimNames.Azp, userInfo.OrganizationName ?? "")
            };

            var token = new JwtSecurityToken(_configuration["Jwt:Issuer"],
              _configuration["Jwt:Audience"],
              claims,
              expires: DateTime.Now.AddMinutes(30),
              signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private static string GenerateJSONWebToken(List<Claim> authClaims, string JWTKey, string JWTIssuer, string JWTAudience)
        {
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: JWTIssuer,
                audience: JWTAudience,
                authClaims,
                expires: DateTime.Now.AddMinutes(30),
                signingCredentials: credentials
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
        private async Task<AdminUserModel?> AuthenticateUser(LoginModel user)
        {
            AdminUserModel? userModel = null;
            string salt = "";

            using (LojClientDbContext db = new(ConnStr))
            {
                User? dbUser = await db.Users.FindAsync(user.Username);
                if (dbUser != null)
                {
                    userModel = new AdminUserModel(
                                dbUser.Login,
                                dbUser.Password,
                                UserModel.ConvertToEnum(dbUser.Type),
                                dbUser.Email,
                                dbUser.Organization
                            );
                    salt = dbUser.Salt;
                    if (string.IsNullOrEmpty(salt))
                    {
                        return null;
                    }
                }
            }

            return (userModel != null && userModel.VerifyPassword(user.GetSecurePassword(), salt)) ? userModel : null;
        }
        private static ClaimsPrincipal? GetPrincipalFromExpiredToken(string? token, string JWTKey)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JWTKey)),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler
            {
                MapInboundClaims = false
            };
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }
    }
}
