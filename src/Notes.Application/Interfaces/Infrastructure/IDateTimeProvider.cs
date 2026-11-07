namespace Notes.Application.Interfaces.Infrastructure
{
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Конвертировать строку в DateTime
        /// </summary>
        /// <param name="dateTime">Строковое представление времени</param>
        /// <returns>DateTime строки. null, если не соответствует ISO 8601, UTC</returns>
        public DateTime? StringToDateTime(string dateTime);

        /// <summary>
        /// Конвертировать DateTime в строку ISO 8601, UTC
        /// </summary>
        /// <param name="dateTime">Исходное DateTime</param>
        /// <returns>Строку формата ISO 8601, UTC</returns>
        public string DateTimeToString(DateTime dateTime);
    }
}
