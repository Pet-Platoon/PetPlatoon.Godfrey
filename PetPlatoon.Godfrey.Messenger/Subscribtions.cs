using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PetPlatoon.Godfrey.Messenger
{
    internal static class Subscriptions
    {
        #region Variables

        private static readonly List<Subscription> AllSubscriptions = new List<Subscription>();
        private static int _subscriptionsChangeCounter;

        [ThreadStatic] private static int _localSubscriptionRevision;

        [ThreadStatic] private static Subscription[] _localSubscriptions;

        #endregion Variables

        #region Methods

        internal static Task<Guid> Register<T>(TimeSpan throttleBy, Func<T, Task> action)
        {
            var type = typeof(T);
            var key = Guid.NewGuid();
            var subscription = new Subscription(type, key, throttleBy, action);

            lock (AllSubscriptions)
            {
                AllSubscriptions.Add(subscription);
                _subscriptionsChangeCounter++;
            }

            return Task.FromResult(key);
        }

        internal static Task UnRegister(Guid token)
        {
            lock (AllSubscriptions)
            {
                var subscription = AllSubscriptions.Find(s => s.Token == token);
                if (subscription == null)
                {
                    return Task.CompletedTask;
                }

                var removed = AllSubscriptions.Remove(subscription);
                if (!removed)
                {
                    return Task.CompletedTask;
                }

                if (_localSubscriptions != null)
                {
                    var localIdx = Array.IndexOf(_localSubscriptions, subscription);
                    if (localIdx >= 0)
                    {
                        _localSubscriptions = RemoveAt(_localSubscriptions, localIdx);
                    }
                }

                _subscriptionsChangeCounter++;
            }

            return Task.CompletedTask;
        }

        internal static Task Clear()
        {
            lock (AllSubscriptions)
            {
                AllSubscriptions.Clear();
                if (_localSubscriptions != null)
                {
                    Array.Clear(_localSubscriptions, 0, _localSubscriptions.Length);
                }

                _subscriptionsChangeCounter++;
            }

            return Task.CompletedTask;
        }

        internal static Task<bool> IsRegistered(Guid token)
        {
            lock (AllSubscriptions)
            {
                return Task.FromResult(AllSubscriptions.Any(s => s.Token == token));
            }
        }

        internal static Task<Subscription[]> GetTheLatestSubscriptions()
        {
            if (_localSubscriptions == null)
            {
                _localSubscriptions = new Subscription[0];
            }

            var changeCounterLatestCopy = Interlocked.CompareExchange(ref _subscriptionsChangeCounter, 0, 0);
            if (_localSubscriptionRevision == changeCounterLatestCopy)
            {
                return Task.FromResult(_localSubscriptions);
            }

            Subscription[] latestSubscriptions;
            lock (AllSubscriptions)
            {
                latestSubscriptions = AllSubscriptions.ToArray();
            }

            _localSubscriptionRevision = changeCounterLatestCopy;
            _localSubscriptions = latestSubscriptions;
            return Task.FromResult(_localSubscriptions);
        }

        private static T[] RemoveAt<T>(T[] source, int index)
        {
            var dest = new T[source.Length - 1];
            if (index > 0)
            {
                Array.Copy(source, 0, dest, 0, index);
            }

            if (index < source.Length - 1)
            {
                Array.Copy(source, index + 1, dest, index, source.Length - index - 1);
            }

            return dest;
        }

        #endregion Methods
    }
}
