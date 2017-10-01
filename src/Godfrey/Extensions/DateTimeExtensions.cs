using System;

namespace Godfrey.Extensions
{
    public static class DateTimeExtensions
    {
        public static string PrettyPrint(this DateTime dateTime)
        {
            var date = dateTime.ToLocalTime();

            return $"{date.Day}.{date.Month}.{date.Year} {date.Hour}:{date.Minute} Uhr";
        }
    }
}
