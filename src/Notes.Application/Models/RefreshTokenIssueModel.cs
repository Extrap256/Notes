namespace Notes.Application.Models
{
    public record RefreshTokenIssueModel(string Token, DateTime ExpiresAt);
}
