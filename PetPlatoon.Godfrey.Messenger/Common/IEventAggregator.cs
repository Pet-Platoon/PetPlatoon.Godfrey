using System;
using System.Threading.Tasks;

namespace PetPlatoon.Godfrey.Messenger.Common
{
    /// <summary>
    ///     Source: https://github.com/NimaAra/Easy.MessageHub
    ///     An implementation of the <c>Event Aggregator</c> pattern.
    /// </summary>
    public interface IEventAggregator : IDisposable
    {
        /// <summary>
        ///     Registers a callback which is invoked on every message published by the <see cref="IEventAggregator" />.
        ///     <remarks>Invoking this method with a new <paramref name="onMessage" />overwrites the previous one.</remarks>
        /// </summary>
        /// <param name="onMessage">
        ///     The callback to invoke on every message
        ///     <remarks>The callback receives the type of the message and the message as arguments</remarks>
        /// </param>
        Task RegisterGlobalHandler(Func<Type, object, Task> onMessage);

        /// <summary>
        ///     Invoked if an error occurs when publishing a message to a subscriber.
        ///     <remarks>Invoking this method with a new <paramref name="onError" />overwrites the previous one.</remarks>
        /// </summary>
        Task RegisterGlobalErrorHandler(Func<Guid, Exception, Task> onError);

        /// <summary>
        ///     Publishes the <paramref name="message" /> on the <see cref="IEventAggregator" />.
        /// </summary>
        /// <param name="message">The message to published</param>
        Task Publish<T>(T message);

        /// <summary>
        ///     Subscribes a callback against the <see cref="IEventAggregator" /> for a specific type of message.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to</typeparam>
        /// <param name="action">
        ///     The callback to be invoked once the message is published on the <see cref="IEventAggregator" />
        /// </param>
        /// <returns>The token representing the subscription</returns>
        Task<Guid> Subscribe<T>(Func<T, Task> action);

        /// <summary>
        ///     Subscribes a callback against the <see cref="EventAggregator" /> for a specific type of message.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to</typeparam>
        /// <param name="action">
        ///     The callback to be invoked once the message is published on the <see cref="EventAggregator" />
        /// </param>
        /// <param name="throttleBy">The <see cref="TimeSpan" /> specifying the rate at which subscription is throttled</param>
        /// <returns>The token representing the subscription</returns>
        Task<Guid> Subscribe<T>(Func<T, Task> action, TimeSpan throttleBy);

        /// <summary>
        ///     Unsubscribes a subscription from the <see cref="IEventAggregator" />.
        /// </summary>
        /// <param name="token">The token representing the subscription</param>
        Task Unsubscribe(Guid token);

        /// <summary>
        ///     Checks if a specific subscription is active on the <see cref="IEventAggregator" />.
        /// </summary>
        /// <param name="token">The token representing the subscription</param>
        /// <returns><c>True</c> if the subscription is active otherwise <c>False</c></returns>
        Task<bool> IsSubscribed(Guid token);

        /// <summary>
        ///     Clears all the subscriptions from the <see cref="EventAggregator" />.
        ///     <remarks>The global handler and the global error handler are not affected</remarks>
        /// </summary>
        Task ClearSubscriptions();
    }
}
