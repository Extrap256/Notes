using System.Globalization;
using Notes.Application.Interfaces.Infrastructure;

namespace Notes.Infrastructure.Services
{
    public class DateTimeProvider : IDateTimeProvider
    {
        readonly string format = "yyyy-MM-ddTHH:mm:ss'Z'";
        public DateTime? StringToDateTime(string dateTime)
        {
            bool success = DateTime.TryParseExact(dateTime,
                format,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                out DateTime result);

            if (success)
                return result;
            else
                return null;
        }

        public string DateTimeToString(DateTime dateTime)
        {
            return dateTime
                .ToUniversalTime()
                .ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
