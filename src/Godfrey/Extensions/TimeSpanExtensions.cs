using System;
using System.Collections.Generic;

namespace Godfrey.Extensions
{
    public static class TimeSpanExtensions
    {
        public static string PrettyPrint(this TimeSpan timeSpan, bool printMilliseconds = false)
        {
            var parts = new List<string>();

            if (timeSpan.Days > 0)
            {
                parts.Add($"{timeSpan.Days} Tag{(timeSpan.Days == 1 ? "" : "e")}");
            }

            if (timeSpan.Hours > 0)
            {
                parts.Add($"{timeSpan.Hours} Stunde{(timeSpan.Hours == 1 ? "" : "n")}");
            }

            if (timeSpan.Minutes > 0)
            {
                parts.Add($"{timeSpan.Minutes} Minute{(timeSpan.Minutes == 1 ? "" : "n")}");
            }

            if (timeSpan.Seconds > 0)
            {
                parts.Add($"{timeSpan.Seconds} Sekunde{(timeSpan.Seconds == 1 ? "" : "n")}");
            }

            if (printMilliseconds && timeSpan.Milliseconds > 0)
            {
                parts.Add($"{timeSpan.Milliseconds} Millisekunden{(timeSpan.Milliseconds == 1 ? "" : "n")}");
            }

            if (parts.Count > 1)
            {
                parts.Insert(parts.Count - 1, "und");
            }

            return parts.Count == 0 ? "0 Sekunden" : string.Join(" ", parts);
        }
    }
}
