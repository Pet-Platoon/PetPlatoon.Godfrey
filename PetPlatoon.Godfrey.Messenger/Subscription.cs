using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace PetPlatoon.Godfrey.Messenger
{
    internal sealed class Subscription
    {
        #region Constructors

        internal Subscription(Type type, Guid token, TimeSpan throttleBy, object handler)
        {
            Type = type;
            Token = token;
            Handler = handler;
            _throttleByTicks = throttleBy.Ticks;
        }

        #endregion Constructors

        #region Variables

        private const long TicksMultiplier = 1000 * TimeSpan.TicksPerMillisecond;
        private readonly long _throttleByTicks;
        private double? _lastHandleTimestamp;

        #endregion Variables

        #region Properties

        internal Guid Token { get; }
        internal Type Type { get; }
        private object Handler { get; }

        #endregion Properties

        #region Methods

        internal async Task Handle<T>(T message)
        {
            if (!await CanHandle())
            {
                return;
            }

            var handler = Handler as Func<T, Task>;
            // ReSharper disable once PossibleNullReferenceException
            await handler(message);
        }
        
        private Task<bool> CanHandle()
        {
            var taskCompletionSource = new TaskCompletionSource<bool>();

            if (_throttleByTicks == 0)
            {
                taskCompletionSource.SetResult(true);
                return taskCompletionSource.Task;
            }

            if (_lastHandleTimestamp == null)
            {
                _lastHandleTimestamp = Stopwatch.GetTimestamp();
                taskCompletionSource.SetResult(true);
                return taskCompletionSource.Task;
            }

            var now = Stopwatch.GetTimestamp();
            var durationInTicks = (now - _lastHandleTimestamp) / Stopwatch.Frequency * TicksMultiplier;

            if (!(durationInTicks >= _throttleByTicks))
            {
                taskCompletionSource.SetResult(false);
                return taskCompletionSource.Task;
            }

            _lastHandleTimestamp = now;
            taskCompletionSource.SetResult(true);
            return taskCompletionSource.Task;
        }

        #endregion Methods
    }
}
