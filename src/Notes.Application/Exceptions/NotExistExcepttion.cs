namespace Notes.Application.Exceptions
{
    /// <summary>
    /// Совершено обращение к элементу, которого нет в базе
    /// </summary>
    public class NotExistExcepttion : Exception
    {
        public NotExistExcepttion() : base() { }
        public NotExistExcepttion(string message) : base(message) { }
        public NotExistExcepttion(string? message, Exception? innerException) : base(message, innerException) { }
    }
}
