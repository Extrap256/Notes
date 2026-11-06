using Notes.Application.Models;

namespace Notes.Application.Interfaces.Application
{
    public interface ITokenService
    {
        /// <summary>
        /// Генерирует новый Access токен
        /// </summary>
        /// <param name="refreshToken">Refresh token</param>
        /// <returns>Пара токенов, включая старый refresh. null, если refresh токен устарел или отстранен</returns>
        public Task<AuthPairTokenModel?> GetAccessToken(string refreshToken);

        /// <summary>
        /// Обновляет пару токенов
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Пара новых токенов или null, если пользователь не существует</returns>
        public Task<AuthPairTokenModel?> GetPairTokens(Guid userId);
    }
}
