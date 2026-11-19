using Notes.Application.Models;

namespace Notes.Application.Interfaces.Infrastructure
{
    public interface ITokenGenerator
    {
        /// <summary>
        /// Генерирует новый Access JWT токен
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Модель access токена</returns>
        public AccessTokenModel GenerateAccessToken(Guid userId);

        /// <summary>
        /// Генерирует новый refresh токен
        /// </summary>
        /// <returns>Модель refresh токена</returns>
        public RefreshTokenIssueModel GenerateRefreshToken();
    }
}
