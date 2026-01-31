using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Backend
{
    public class DatetimeUtcConverter : ValueConverter<DateTime, DateTime>
    {
        public DatetimeUtcConverter() : base(
            v => v.ToUniversalTime(),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc))
        {
        }
    }
}
