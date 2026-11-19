using Notes.Application.Models;

namespace Notes.Application.Interfaces.Application
{
    public interface ITokenService
    {
        /// <summary>
        /// Обновляет токены
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Пара новых токенов. null, если refresh токен устарел или отстранен</returns>
        public Task<AuthPairTokenModel?> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Создает пару новых токенов
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Пара новых токенов или null, если пользователь не существует</returns>
        public Task<AuthPairTokenModel?> IssueTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
