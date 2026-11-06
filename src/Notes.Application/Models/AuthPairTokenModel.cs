namespace Notes.Application.Models
{
    public record AuthPairTokenModel(
        string AccessToken,
        DateTime AccessTokenExpiresAt,
        string RefreshToken,
        DateTime RefreshTokenExpiresAt
        );
}
