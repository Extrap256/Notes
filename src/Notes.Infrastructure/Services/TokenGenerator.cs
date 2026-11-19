using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Notes.Application.Models;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;

namespace Notes.Infrastructure.Services
{
    public class TokenGenerator(IConfiguration configuration) : ITokenGenerator
    {
        private readonly IConfiguration _configuration = configuration;
        public AccessTokenModel GenerateAccessToken(Guid userId)
        {
            DateTime expiresAt = DateTime.UtcNow.AddMinutes(30);

            var claims = new Claim[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Secret"]!));

            var credintioals = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expiresAt,
                signingCredentials: credintioals
                );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return new AccessTokenModel(tokenString, expiresAt);
        }

        public string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }
    }
}
