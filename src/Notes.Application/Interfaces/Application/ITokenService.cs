using Notes.Application.Models;

namespace Notes.Application.Interfaces.Application
{
    public interface ITokenService
    {
        /// <summary>
        /// Генерирует новый Access токен
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Пара новых токенов. null, если refresh токен устарел или отстранен</returns>
        public Task<AuthPairTokenModel?> GetAccessTokenAsync(string refreshToken, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновляет пару токенов
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Пара новых токенов или null, если пользователь не существует</returns>
        public Task<AuthPairTokenModel?> GetPairTokensAsync(Guid userId, CancellationToken cancellationToken = default);
    }
}
