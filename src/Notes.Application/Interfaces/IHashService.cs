namespace Notes.Application.Interfaces
{
    public interface IHashService
    {
        /// <summary>
        /// Хеширование строки
        /// </summary>
        /// <param name="payload">Хешируемая строка</param>
        /// <returns>Хеш строки</returns>
        public string Hash(string payload);
    }
}
