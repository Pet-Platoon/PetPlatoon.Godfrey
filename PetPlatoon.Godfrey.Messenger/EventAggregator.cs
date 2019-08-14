using System;
using System.Threading.Tasks;
using JetBrains.Annotations;
using PetPlatoon.Godfrey.Messenger.Common;

namespace PetPlatoon.Godfrey.Messenger
{
    /// <summary>
    ///     Source: https://github.com/NimaAra/Easy.MessageHub
    ///     An implementation of the <c>Event Aggregator</c> pattern.
    /// </summary>
    public class EventAggregator : IEventAggregator
    {
        #region Constructors

        private EventAggregator()
        {
        }

        #endregion Constructors

        #region Properties

        public static EventAggregator Instance { get; } = new EventAggregator();

        #endregion Properties

        #region Variables

        private Func<Type, object, Task> _globalHandler;

        private Func<Guid, Exception, Task> _globalErrorHandler;

        #endregion Variables

        #region Methods

        public Task RegisterGlobalHandler([NotNull] Func<Type, object, Task> onMessage)
        {
            _globalHandler = onMessage;

            return Task.CompletedTask;
        }

        public Task RegisterGlobalErrorHandler([NotNull] Func<Guid, Exception, Task> onError)
        {
            _globalErrorHandler = onError;

            return Task.CompletedTask;
        }

        public async Task Publish<T>([NotNull] T message)
        {
            var localSubscriptions = await Subscriptions.GetTheLatestSubscriptions();

            var msgType = typeof(T);
            if (_globalHandler != null)
            {
                await _globalHandler.Invoke(msgType, message);
            }

            foreach (var subscription in localSubscriptions)
            {
                if (!subscription.Type.IsAssignableFrom(msgType))
                {
                    continue;
                }

                try
                {
                    await subscription.Handle(message);
                }
                catch (Exception e)
                {
                    if (_globalErrorHandler != null)
                    {
                        await _globalErrorHandler.Invoke(subscription.Token, e);
                    }
                }
            }
        }

        public Task<Guid> Subscribe<T>([NotNull] Func<T, Task> action)
        {
            return Subscribe(action, TimeSpan.Zero);
        }

        public Task<Guid> Subscribe<T>([NotNull] Func<T, Task> action, TimeSpan throttleBy)
        {
            return Subscriptions.Register(throttleBy, action);
        }

        public Task Unsubscribe(Guid token)
        {
            return Subscriptions.UnRegister(token);
        }

        public Task<bool> IsSubscribed(Guid token)
        {
            return Subscriptions.IsRegistered(token);
        }

        public Task ClearSubscriptions()
        {
            return Subscriptions.Clear();
        }

        public void Dispose()
        {
            _globalHandler = null;
            ClearSubscriptions();
        }

        #endregion Methods
    }
}
