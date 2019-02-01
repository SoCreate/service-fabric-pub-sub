using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    /// <remarks>
    /// Base class for a <see cref="StatelessService"/> that serves as a subscriber of messages from the broker.
    /// Subscribe to message types and define a handler callback by calling <see cref="SubscriberStatelessServiceBase.RegisterHandler{T}"/>.
    /// </remarks>
    public abstract class SubscriberStatelessServiceBase : StatelessService, ISubscriberStatelessService
    {
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;

        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        protected Action<string> ServiceEventSourceMessageCallback { get; set; }

        /// <summary>
        /// Dictionary of <see cref="SubscriptionDefinition"/>, keyed by the message type name, that this service subscribes to.
        /// </summary>
        private readonly Dictionary<string, SubscriptionDefinition> _subscriptions = new Dictionary<string, SubscriptionDefinition>();

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext)
            : base(serviceContext)
        {
            _subscriberServiceHelper = new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <summary>
        /// Subscribes to all messages that have been registered by calling <see cref="SubscriberStatelessServiceBase.RegisterHandler{T}"/>.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override async Task OnOpenAsync(CancellationToken cancellationToken)
        {
            var serviceName = GetType().Namespace;

            foreach (var subscription in _subscriptions.Values)
            {
                try
                {
                    await SubscribeAsync(subscription.MessageType);
                    ServiceEventSourceMessage($"Registered Service:'{serviceName}' Instance:'{Context.InstanceId}' as Subscriber of {subscription.MessageType}.");
                }
                catch (Exception ex)
                {
                    ServiceEventSourceMessage($"Failed to register Service:'{serviceName}' Instance:'{Context.InstanceId}' as Subscriber of {subscription.MessageType}. Error:'{ex}'.");
                }
            }
        }

        /// <summary>
        /// Registers this service as a subscriber for the given message type.
        /// </summary>
        /// <param name="messageType">Full type of message object.</param>
        public async Task SubscribeAsync(Type messageType)
        {
            if (messageType.FullName != null && _subscriptions[messageType.FullName] is SubscriptionDefinition subscription)
            {
                await _subscriberServiceHelper.RegisterMessageTypeAsync(this, messageType, subscription.Broker);
            }
        }

        /// <summary>
        /// Unsubscribes service from a given message type.
        /// </summary>
        /// <param name="messageType">Full type of message object.</param>
        /// <param name="flush">Publish any remaining messages before unsubscribing.</param>
        public async Task UnsubscribeAsync(Type messageType, bool flush = true)
        {
            if (messageType.FullName != null && _subscriptions[messageType.FullName] is SubscriptionDefinition subscription)
            {
                await _subscriberServiceHelper.UnregisterMessageTypeAsync(this, messageType, flush, subscription.Broker);
                _subscriptions.Remove(messageType.FullName);
            }
        }

        /// <summary>
        /// Receives a published message using the handler registered for the given type.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task ReceiveMessageAsync(MessageWrapper message)
        {
            var subscription = _subscriptions[message.MessageType];
            await subscription.Handler(_subscriberServiceHelper.Deserialize(message, subscription.MessageType));
        }

        /// <summary>
        /// Registers a handler for a given message type (T).
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="broker"></param>
        public SubscriberStatelessServiceBase RegisterHandler<T>(Func<T, Task> handler, Uri broker = null) where T : class
        {
            var messageType = typeof(T);
            if (messageType.FullName != null)
            {
                _subscriptions[messageType.FullName] = new SubscriptionDefinition
                {
                    Broker = broker,
                    MessageType = messageType,
                    Handler = async message => await handler(message as T)
                };
            }

            return this;
        }

        /// <inheritdoc/>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }

        /// <summary>
        /// Outputs the provided message to the <see cref="ServiceEventSourceMessageCallback"/> if it's configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        protected void ServiceEventSourceMessage(string message, [CallerMemberName] string caller = "unknown")
        {
            ServiceEventSourceMessageCallback?.Invoke($"{caller} - {message}");
        }

        private class SubscriptionDefinition
        {
            public Uri Broker { get; set; }
            public Type MessageType { get; set; }
            public Func<object, Task> Handler { get; set; }
        }
    }
}