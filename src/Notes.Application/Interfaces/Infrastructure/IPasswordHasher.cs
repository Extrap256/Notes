namespace Notes.Application.Interfaces.Infrastructure
{
    public interface IPasswordHasher
    {
        /// <summary>
        /// Хеширует пароль с солью
        /// </summary>
        /// <param name="password">Пароль для хеширования с солью</param>
        /// <returns>Строка хеша</returns>
        public string Hash(string password);

        /// <summary>
        /// Проверет соответствие пароля хешу
        /// </summary>
        /// <param name="password">Введенный пароль</param>
        /// <param name="hash">Существующий хеш</param>
        /// <returns>true, если подходит, иначе false</returns>
        public bool Verify(string password, string hash);
    }
}
