using Notes.Application.Models;

namespace Notes.Application.Interfaces.Application
{
    internal interface IAuthService
    {
        /// <summary>
        /// Зарегистрировать нового пользователя
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns>true, если успешно. false, если логин уже занят</returns>
        public Task<RegisterResult> RegisterAsync(string login, string password, CancellationToken cancellationToken = default);

        /// <summary>
        /// Войти пользователю
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns></returns>
        public Task<LoginResult> LoginAsync(string login, string password, CancellationToken cancellationToken = default);
    }
}
