using Notes.Application.Interfaces.Infrastructure;

namespace Notes.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
