
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RestApiNExApplication.Api.Attributes;
using RestApiNExApplication.Domain;
using RestApiNExApplication.Domain.Service;
using RestApiNExApplication.Entity;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JWT.Controllers
{
    [ApiVersion("1.0")]
    [Route("api/[controller]")]
    //[Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public class TokenController : Controller
    {
        private IConfiguration _config;
        private readonly UserServiceAsync<UserViewModel, User> _userServiceAsync;

        public TokenController(IConfiguration config, UserServiceAsync<UserViewModel, User> userServiceAsync)
        {
            _config = config;
            _userServiceAsync = userServiceAsync;
        }

        [AllowAnonymous]
        [HttpPost]
        //[DDosAttackProtected]
        public async Task<IActionResult> Create([FromBody] LoginModel login)
        {
            IActionResult response = Unauthorized();

            var user = await Authenticate(login);
            user.Account = null;
            JwtSecurityToken token;

            if (user != null)
            {
                user.Account = null;
                token = BuildToken(GetUserModelFromUserViewModel(user));

                var newRefreshToken = GenerateRefreshToken();
                int refreshTokenExpiryDays;
                Int32.TryParse(_config["Jwt:RefreshTokenValidDays"], out refreshTokenExpiryDays);

                user.RefreshToken = newRefreshToken;
                user.RefreshTokenExpiryDateTime = DateTime.Now.AddDays(refreshTokenExpiryDays);
                await _userServiceAsync.SetUserToken(user.Id, user.RefreshToken, user.RefreshTokenExpiryDateTime);
                //await _userServiceAsync.Update(user);

                response = Ok(
                    new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(token),
                        tokenExpiration = token.ValidTo,
                        refreshToken = newRefreshToken
                    }
                 );
                return response;
            }

            return BadRequest("Invalid username or specified username is not active.");

        }

        [Authorize]
        [HttpPost]
        [Route("refresh")]
        public async Task<IActionResult> RefreshToken(Token refreshToken)
        {

            var token = Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
            if (token is null)
            {
                return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Refresh token failed.Access token empty.");
            }

            var principal = GetPrincipalFromToken(token);
            if (principal == null || principal.Identity == null)
            {
                return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Refresh token failed.Invalid access token.");
            }

            string username = principal.Identity.Name ?? "";
            var user = (await _userServiceAsync.Get(x => x.UserName.ToLower() == username.ToLower())).SingleOrDefault();

            if (user == null || user.RefreshToken != refreshToken.refreshToken || user.RefreshTokenExpiryDateTime <= DateTime.Now)
            {
                return StatusCode((int)System.Net.HttpStatusCode.Unauthorized, "Refresh token failed.Invalid refresh token or refresh token expired.");
            }

            //
            var newToken = BuildToken(GetUserModelFromUserViewModel(user));
            var newRefreshToken = GenerateRefreshToken();

            int refreshTokenExpiryDays;
            Int32.TryParse(_config["Jwt:RefreshTokenValidDays"], out refreshTokenExpiryDays);

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryDateTime = DateTime.Now.AddDays(refreshTokenExpiryDays);
            await _userServiceAsync.SetUserToken(user.Id, user.RefreshToken, user.RefreshTokenExpiryDateTime);
            //await _userServiceAsync.Update(user);

            IActionResult response = Ok(
                new
                {
                    token = new JwtSecurityTokenHandler().WriteToken(newToken),
                    tokenExpiration = newToken.ValidTo,
                    refreshToken = newRefreshToken
                }
             );

            return response;
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [Route("revoke/{username}")]
        public async Task<IActionResult> Revoke(string username)
        {
            username = username ?? "";
            var user = (await _userServiceAsync.Get(x => x.UserName.ToLower() == username.ToLower())).SingleOrDefault();
            if (user == null) return BadRequest("Invalid user name");

            user.RefreshToken = string.Empty;
            user.RefreshTokenExpiryDateTime = DateTime.Now;
            await _userServiceAsync.SetUserToken(user.Id, user.RefreshToken, user.RefreshTokenExpiryDateTime);
            //await _userServiceAsync.Update(user);

            return NoContent();
        }

        [Authorize(Roles = "Administrator")]
        [HttpPost]
        [Route("revoke-all")]
        public async Task<IActionResult> RevokeAll()
        {
            var users = (await _userServiceAsync.GetAll()).ToList();
            foreach (var user in users)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryDateTime = DateTime.MinValue;
                await _userServiceAsync.Update(user);
            }

            return NoContent();
        }

        private JwtSecurityToken BuildToken(UserModel user)
        {

            List<Claim> claims = new List<Claim>();
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.Username));
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Name));
            claims.Add(new Claim(JwtRegisteredClaimNames.Email, user.Email));
            claims.Add(new Claim(JwtRegisteredClaimNames.Birthdate, user.Birthdate.ToString("yyyy-MM-dd")));
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()));
            //attach roles
            foreach (string role in user.Roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            int timeout = 3600;
            Int32.TryParse(_config["Jwt:TokenValidMinutes"], out timeout);
            var token = new JwtSecurityToken(
               _config["Jwt:Issuer"],
               _config["Jwt:Issuer"],
               claims,
              expires: DateTime.Now.AddMinutes(timeout),  //timeout mins expiry and a client monitors token quality and should request new token when this one expiries
              signingCredentials: creds);

            return token;
        }

        //Authenticates login information, retrieves authorization infomation (roles)
        //for active user
        private async Task<UserViewModel> Authenticate(LoginModel login)
        {
            string username = login.Username.ToLower() ?? "";
            var userView = (await _userServiceAsync.Get(x => x.UserName.ToLower() == username && x.IsActive)).SingleOrDefault();
            if (userView != null && userView.Password != login.Password)
                userView = null;

            return userView;
        }

        private UserModel GetUserModelFromUserViewModel(UserViewModel userView)
        {
            UserModel user = new UserModel { Username = userView.UserName, Name = userView.FirstName + " " + userView.LastName, Email = userView.Email, Roles = new string[] { } };
            List<string> roles = new List<string> { };
            foreach (string role in userView.Roles)
            {
                roles.Add(role);
            }
            //add Admin if missing
            if (userView.IsAdminRole && !roles.Contains("Administrator"))
                roles.Add("Administrator");
            user.Roles = roles.ToArray();
            return user;
        }

        private static string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }

        private ClaimsPrincipal GetPrincipalFromToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["JWT:Key"])),
                ValidateLifetime = false
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;

        }

        public class Token
        {
            public string token { get; set; }
            public string refreshToken { get; set; }
            public DateTime tokenExpiration { get; set; }
        }

        public class LoginModel
        {
            public string Username { get; set; }
            public string Password { get; set; }
        }

        private class UserModel
        {
            public string Username { get; set; }
            public string Name { get; set; }
            public string Email { get; set; }
            public DateTime Birthdate { get; set; }
            public string[] Roles { get; set; }
        }
    }
}