namespace Notes.Application.Exceptions
{
    /// <summary>
    /// Совершено обращение к элементу, которого нет в базе
    /// </summary>
    public class NotExistException : Exception
    {
        public NotExistException() : base() { }
        public NotExistException(string message) : base(message) { }
        public NotExistException(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
