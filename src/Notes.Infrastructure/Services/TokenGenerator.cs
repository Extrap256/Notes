using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Notes.Application.Interfaces.Infrastructure;
using Notes.Application.Models;
using Notes.Infrastructure.Exceptions;

namespace Notes.Infrastructure.Services
{
    public sealed class TokenGenerator : ITokenGenerator
    {
        private const int RefreshTokenBytes = 64;

        private readonly IDateTimeProvider _clock;
        private readonly SigningCredentials _credentials;
        private readonly JwtSecurityTokenHandler _handler = new();
        private readonly string _issuer;
        private readonly string _audience;
        private readonly int _accessTokenMinutes;
        private readonly int _refreshTokenDays;

        public TokenGenerator(IConfiguration configuration, IDateTimeProvider clock)
        {
            _clock = clock;

            var secret = configuration["Jwt:Secret"]
                ?? throw new ConfigurationException("Не задан Jwt:Secret");

            var keyBytes = Encoding.UTF8.GetBytes(secret);
            if (keyBytes.Length < 32)
                throw new ConfigurationException(
                    "Jwt:Secret должен быть не короче 32 байт для HS256");

            _credentials = new SigningCredentials(
                new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256);

            _issuer = configuration["Jwt:Issuer"]
                ?? throw new ConfigurationException("Не задан Jwt:Issuer");
            _audience = configuration["Jwt:Audience"]
                ?? throw new ConfigurationException("Не задан Jwt:Audience");

            _accessTokenMinutes = ReadPositiveInt(configuration["Jwt:AccessTokenMinutes"], 30);
            _refreshTokenDays = ReadPositiveInt(configuration["Jwt:RefreshTokenDays"], 7);
        }

        public AccessTokenModel GenerateAccessToken(Guid userId)
        {
            var now = _clock.UtcNow;
            var expiresAt = now.AddMinutes(_accessTokenMinutes);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _issuer,
                audience: _audience,
                claims: claims,
                notBefore: now,
                expires: expiresAt,
                signingCredentials: _credentials);

            return new AccessTokenModel(_handler.WriteToken(token), expiresAt);
        }

        public RefreshTokenIssueModel GenerateRefreshToken()
        {
            var token = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(RefreshTokenBytes));
            var expiresAt = _clock.UtcNow.AddDays(_refreshTokenDays);

            return new RefreshTokenIssueModel(token, expiresAt);
        }

        private static int ReadPositiveInt(string? raw, int fallback)
            => int.TryParse(raw, out var value) && value > 0 ? value : fallback;
    }
}