namespace Notes.Application.Interfaces.Infrastructure
{
    public interface ITokenHasher
    {
        /// <summary>
        /// Хеширует токен
        /// </summary>
        /// <param name="token">Токен</param>
        /// <returns>Захешированная строка токена без соли</returns>
        public string Hash(string token);
    }
}
