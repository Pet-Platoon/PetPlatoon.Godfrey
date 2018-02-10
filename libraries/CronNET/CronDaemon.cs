using System;
using System.Collections.Generic;
using System.Threading;
using System.Timers;
using Timer = System.Timers.Timer;

namespace CronNET
{
    public interface ICronDaemon
    {
        void AddJob(string schedule, ThreadStart action);
        void Start();
        void Stop();
    }

    public class CronDaemon : ICronDaemon
    {
        private readonly Timer _timer = new Timer(TimeSpan.FromSeconds(30).TotalMilliseconds);
        private readonly ICollection<ICronJob> _cronJobs = new List<ICronJob>();
        private DateTime _last = DateTime.UtcNow;

        public CronDaemon()
        {
            _timer.AutoReset = true;
            _timer.Elapsed += TimerOnElapsed;
        }

        private void TimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (DateTime.UtcNow.Minute != _last.Minute)
            {
                _last = DateTime.UtcNow;

                foreach (var cronJob in _cronJobs)
                {
                    cronJob.Execute(DateTime.UtcNow);
                }
            }
        }

        public void AddJob(string schedule, ThreadStart action)
        {
            var cron = new CronJob(schedule, action);
            _cronJobs.Add(cron);
        }

        public void Start()
        {
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();

            foreach (var cronJob in _cronJobs)
            {
                cronJob.Abort();
            }
        }
    }
}
