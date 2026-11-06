using Notes.Application.DTO;
using Notes.Application.Models;
using Notes.Domain.Entities;

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
        public Task<RegisterResult> Register(string login, string password);

        /// <summary>
        /// Войти пользователю
        /// </summary>
        /// <param name="login">Логин пользователя</param>
        /// <param name="password">Пароль пользователя</param>
        /// <returns></returns>
        public Task<LoginResult> Login(string login, string password);
    }
}
