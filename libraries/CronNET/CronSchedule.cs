using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CronNET
{
    public interface ICronSchedule
    {
        bool IsValid(string expression);
        bool IsTime(DateTime dateTime);
    }

    public class CronSchedule : ICronSchedule
    {
        #region Properties

        private static readonly Regex DividedRegex = new Regex(@"(\*/\d+)");
        private static readonly Regex RangeRegex = new Regex(@"(\d+\-\d+)\/?(\d+)?");
        private static readonly Regex WildRegex = new Regex(@"(\*)");
        private static readonly Regex ListRegex = new Regex(@"(((\d+,)*\d+)+)");
        private static readonly Regex ValidationRegex = new Regex(DividedRegex + "|" + RangeRegex + "|" + WildRegex + "|" + ListRegex);
        private readonly string _expression;
        public List<int> Minutes;
        public List<int> Hours;
        public List<int> DaysOfMonth;
        public List<int> Months;
        public List<int> DaysOfWeek;

        #endregion Properties

        public CronSchedule()
        {

        }

        public CronSchedule(string expression)
        {
            _expression = expression;
            Generate();
        }

        public bool IsValid()
        {
            return IsValid(_expression);
        }

        public bool IsValid(string expression)
        {
            var matches = ValidationRegex.Matches(expression);
            return matches.Count > 0;
        }

        public bool IsTime(DateTime dateTime)
        {
            return Minutes.Contains(dateTime.Minute) && Hours.Contains(dateTime.Hour) &&
                   DaysOfMonth.Contains(dateTime.Day) && Months.Contains(dateTime.Month) &&
                   DaysOfWeek.Contains((int)dateTime.DayOfWeek);
        }

        private void Generate()
        {
            if (!IsValid())
            {
                return;
            }

            var matches = ValidationRegex.Matches(_expression);

            GenerateMinutes(matches[0].ToString());

            if (matches.Count > 1)
            {
                GenerateHours(matches[1].ToString());
            }
            else
            {
                GenerateHours("*");
            }

            if (matches.Count > 2)
            {
                GenerateDaysOfMonth(matches[2].ToString());
            }
            else
            {
                GenerateDaysOfMonth("*");
            }

            if (matches.Count > 3)
            {
                GenerateMonths(matches[3].ToString());
            }
            else
            {
                GenerateMonths("*");
            }

            if (matches.Count > 4)
            {
                GenerateDaysOfWeeks(matches[4].ToString());
            }
            else
            {
                GenerateDaysOfWeeks("*");
            }
        }

        private void GenerateMinutes(string match)
        {
            Minutes = GenerateValues(match, 0, 60);
        }

        private void GenerateHours(string match)
        {
            Hours = GenerateValues(match, 0, 24);
        }

        private void GenerateDaysOfMonth(string match)
        {
            DaysOfMonth = GenerateValues(match, 1, 32);
        }

        private void GenerateMonths(string match)
        {
            Months = GenerateValues(match, 1, 13);
        }

        private void GenerateDaysOfWeeks(string match)
        {
            DaysOfWeek = GenerateValues(match, 0, 7);
        }

        private List<int> GenerateValues(string configuration, int start, int max)
        {
            if (DividedRegex.IsMatch(configuration))
            {
                return DividedArray(configuration, start, max);
            }

            if (RangeRegex.IsMatch(configuration))
            {
                return RangeArray(configuration);
            }

            if (WildRegex.IsMatch(configuration))
            {
                return WildArray(configuration, start, max);
            }

            if (ListRegex.IsMatch(configuration))
            {
                return ListArray(configuration);
            }

            return new List<int>();
        }

        private List<int> DividedArray(string configuration, int start, int max)
        {
            if (!DividedRegex.IsMatch(configuration))
            {
                return new List<int>();
            }

            var ret = new List<int>();
            var split = configuration.Split("/".ToCharArray());
            var divisor = int.Parse(split[1]);

            for (var i = start; i < max; ++i)
            {
                if (i % divisor == 0)
                {
                    ret.Add(i);
                }
            }

            return ret;
        }

        private List<int> RangeArray(string configuration)
        {
            if (!RangeRegex.IsMatch(configuration))
            {
                return new List<int>();
            }

            var ret = new List<int>();
            var split = configuration.Split("-".ToCharArray());
            var start = int.Parse(split[0]);
            int end;
            if (split[1].Contains("/"))
            {
                split = split[1].Split("/".ToCharArray());
                end = int.Parse(split[0]);
                var divisor = int.Parse(split[1]);

                for (var i = start; i < end; ++i)
                {
                    if (i % divisor == 0)
                    {
                        ret.Add(i);
                    }
                }

                return ret;
            }

            end = int.Parse(split[1]);

            for (var i = start; i <= end; ++i)
            {
                ret.Add(i);
            }

            return ret;
        }

        private List<int> WildArray(string configuration, int start, int max)
        {
            if (!WildRegex.IsMatch(configuration))
            {
                return new List<int>();
            }

            var ret = new List<int>();

            for (var i = start; i < max; ++i)
            {
                ret.Add(i);
            }

            return ret;
        }

        private List<int> ListArray(string configuration)
        {
            if (!ListRegex.IsMatch(configuration))
            {
                return new List<int>();
            }

            var ret = new List<int>();

            var split = configuration.Split(",".ToCharArray());

            foreach (var s in split)
            {
                ret.Add(int.Parse(s));
            }

            return ret;
        }
    }
}
