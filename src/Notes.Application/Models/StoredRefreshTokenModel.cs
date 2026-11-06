namespace Notes.Application.DTO
{
    /// <summary>
    /// Модель для refresh токена
    /// </summary>
    /// <param name="UserId">Id владельца</param>
    /// <param name="Hash">Хеш самого токена</param>
    /// <param name="ExpiresAt">ДатаВремя истекания</param>
    /// <param name="IsRevoked">Аннулирован</param>
    public record StoredRefreshTokenModel(
        Guid UserId,
        string Hash,
        DateTime ExpiresAt,
        bool IsRevoked);
}
