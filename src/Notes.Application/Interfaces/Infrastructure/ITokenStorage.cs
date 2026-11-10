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
        public Task<bool> AddTokenAsync(RefreshTokenModel token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все активные токены пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список всех активных токенов пользователя</returns>
        public Task<List<RefreshTokenModel>> GetAllActiveTokensAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Получить все токены пользователя
        /// </summary>
        /// <param name="userId">Id пользователя</param>
        /// <returns>Список всех токенов пользователя</returns>
        public Task<List<RefreshTokenModel>> GetAllTokensAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Отозвать токен
        /// </summary>
        /// <param name="token">Токен, который нужно отозвать</param>
        /// <returns>true, если отозвано успешно. false, если токен уже был отозван. null, если токена не существует</returns>
        public Task<bool?> RevokeAsync(RefreshTokenModel token, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит refresh токен в базе
        /// </summary>
        /// <param name="hash">Хеш токена</param>
        /// <returns>Модель токена или null, если токен не найден</returns>
        public Task<RefreshTokenModel?> GetActiveTokenByHashAsync(string hash, CancellationToken cancellationToken = default);
    }
}
