using System;

namespace PetPlatoon.Godfrey.Extensions
{
    public static class DateTimeExtensions
    {
        public static string PrettyPrint(this DateTime dateTime)
        {
            var date = dateTime.ToString("R");
            return date;
        }
    }
}
