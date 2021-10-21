using Application.Dto;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Presentation.API.Settings;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace Presentation.API.Controllers
{
    [AllowAnonymous]
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    public class ClientAuthenticationController : ControllerBase
    {
        private readonly string JwtKey;
        private readonly string JwtIssuer;
        private readonly string JwtAudience;

        public ClientAuthenticationController(IOptions<AuthenticationSettings> options)
        {
            this.JwtKey = options.Value.JwtKey;
            this.JwtIssuer = options.Value.JwtIssuer;
            this.JwtAudience = options.Value.JwtAudience;
        }

        [HttpPost("/login")]
        [ProducesResponseType(statusCode: StatusCodes.Status200OK)]
        [ProducesResponseType(statusCode: StatusCodes.Status401Unauthorized)]
        public IActionResult Login([FromBody] UserAuthenticationDto userAuthentication)
        {
            var user = AuthenticateUser(userAuthentication);

            if (user != null)
            {
                var tokenString = GenerateJwt();

               return this.Ok(
                    new { token = $"Bearer {tokenString}" }
                );
            }

            return this.Unauthorized();
        }

        private string GenerateJwt()
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(JwtKey));

            var credentials = new SigningCredentials(
                securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                JwtIssuer,
                JwtAudience,
                null,
                expires: DateTime.Now.AddHours(5),
                signingCredentials: credentials);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private UserAuthenticationDto AuthenticateUser(UserAuthenticationDto userAuthentication)
        {
            UserAuthenticationDto user = null;

            if (userAuthentication.UserName == "checkout")
            {
                user = new UserAuthenticationDto
                {
                    UserName = "Checkout Demo User"
                };
            }

            return user;
        }
    }
}
