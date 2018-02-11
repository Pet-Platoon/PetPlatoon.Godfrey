using System.Threading;

namespace Godfrey.Scheduler
{
    public abstract class Job
    {
        public abstract bool IsRepeatable { get; }

        public void ExecuteJob()
        {
            if (!IsRepeatable)
            {
                DoJob();
                return;
            }

            while (true)
            {
                DoJob();
                Thread.Sleep(IntervalTime);
            }
        }
    }
}
