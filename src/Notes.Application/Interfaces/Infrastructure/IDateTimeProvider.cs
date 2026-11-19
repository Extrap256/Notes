namespace Notes.Application.Interfaces.Infrastructure
{
    public interface IDateTimeProvider
    {
        /// <summary>
        /// Текущее время
        /// </summary>
        public DateTime UtcNow {  get; }
    }
}
