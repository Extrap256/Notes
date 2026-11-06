using Notes.Application.DTO;

namespace Notes.Application.Interfaces.Infrastructure
{
    public interface ITokenStorage
    {
        /// <summary>
        /// Добавить токен
        /// </summary>
        /// <param name="token">Новый Refresh токен</param>
        /// <returns>true, если успешно. false, если уже токен с таким хешем уже есть</returns>
        public Task<bool> AddToken(RefreshTokenModel token);

        /// <summary>
        /// Получить все активные токены пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список всех активных токенов пользователя. null в случае отсутствия ползователя</returns>
        public Task<List<RefreshTokenModel>?> GetAllActiveTokens(Guid userId);

        /// <summary>
        /// Получить все токены пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список всех токенов пользователя. null в случае отсутствия ползователя</returns>
        public Task<List<RefreshTokenModel>?> GetAllTokens(Guid userId);

        /// <summary>
        /// Отозвать токен
        /// </summary>
        /// <param name="token">Токен, который нужно отозвать</param>
        /// <returns>true, если отозвано успешно. false, если токен уже был отозван. null, если токена не существует</returns>
        public Task<bool?> Revoke(RefreshTokenModel token);
    }
}
