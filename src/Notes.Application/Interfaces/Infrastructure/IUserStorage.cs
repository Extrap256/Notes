using Notes.Domain.Entities;

namespace Notes.Application.Interfaces.Infrastructure
{
    public interface IUserStorage
    {
        /// <summary>
        /// Добавляет пользователя
        /// </summary>
        /// <param name="user">Объект пользователя</param>
        /// <returns>true, если добавление успешно и false, если пользователь с таким id уже существует</returns>
        public Task<bool> AddUserAsync(User user, CancellationToken cancellationToken = default);

        /// <summary>
        /// Находит пользователя
        /// </summary>
        /// <param name="userId">Id нужного пользователя</param>
        /// <returns>Объект пользователя или null в случае его отсутствия</returns>
        public Task<User?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Обновить пользователя
        /// </summary>
        /// <param name="userId">Id нужного пользователя</param>
        /// <param name="passwordHash">Новый хеш пароля</param>
        /// <returns>true, если пользователь успешно обновлен. false, если изменений не было. null, если пользователь не существует</returns>
        public Task<bool?> UpdateUserAsync(Guid userId, string passwordHash, CancellationToken cancellationToken = default);
    }
}
