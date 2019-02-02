using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

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
        protected Dictionary<string, SubscriptionDefinition> Subscriptions { get; } = new Dictionary<string, SubscriptionDefinition>();

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null)
            : base(serviceContext)
        {
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <summary>
        /// Subscribes to all messages that have been registered by calling <see cref="SubscriberStatelessServiceBase.RegisterHandler{T}"/>.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override async Task OnOpenAsync(CancellationToken cancellationToken)
        {
            DiscoverHandlers();
            var serviceName = GetType().FullName;

            foreach (var subscription in Subscriptions.Values)
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
        public Task SubscribeAsync(Type messageType)
        {
            if (messageType.FullName != null && Subscriptions.TryGetValue(messageType.FullName, out var subscription))
            {
                return _subscriberServiceHelper.RegisterMessageTypeAsync(this, messageType, subscription.Broker);
            }

            return Task.FromResult(true);
        }

        /// <summary>
        /// Unsubscribes service from a given message type.
        /// </summary>
        /// <param name="messageType">Full type of message object.</param>
        /// <param name="flush">Publish any remaining messages before unsubscribing.</param>
        public async Task UnsubscribeAsync(Type messageType, bool flush = true)
        {
            if (messageType.FullName != null && Subscriptions.TryGetValue(messageType.FullName, out var subscription))
            {
                await _subscriberServiceHelper.UnregisterMessageTypeAsync(this, messageType, flush, subscription.Broker);
                Subscriptions.Remove(messageType.FullName);
            }
        }

        /// <summary>
        /// Receives a published message using the handler registered for the given type.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public Task ReceiveMessageAsync(MessageWrapper messageWrapper)
        {
            var subscription = Subscriptions[messageWrapper.MessageType];
            return subscription.Handler(messageWrapper.CreateMessage());
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
                Subscriptions[messageType.FullName] = new SubscriptionDefinition
                {
                    Broker = broker,
                    MessageType = messageType,
                    Handler = message => handler((T)message)
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

        /// <summary>
        /// Scans this service for attributes of type <see cref="SubscribeAttribute"/> and corresponding methods that can handle 
        /// messages. 
        /// </summary>
        internal void DiscoverHandlers()
        {
            Type taskType = typeof(Task);
            var methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            var handlesAttributes = GetType().GetCustomAttributes(typeof(SubscribeAttribute), false).Cast<SubscribeAttribute>();
            foreach (var attribute in handlesAttributes)
            {
                var target = methods.SingleOrDefault(m =>
                {
                    var parameters = m.GetParameters();
                    var argument = parameters.FirstOrDefault();
                    return parameters.Length == 1 
                           && m.ReturnType == taskType 
                           && argument != null 
                           && argument.ParameterType == attribute.MessageType;
                });
                if (target == null)
                {
                    throw new InvalidOperationException($"Service {GetType().FullName} is marked with a SubscribeAttribute for MessageType {attribute.MessageType}, but it does not have an async method that handles it.");
                }

                Subscriptions[attribute.MessageType.FullName] = new SubscriptionDefinition
                {
                    MessageType = attribute.MessageType,
                    Handler = m => (Task)target.Invoke(this, new[]{m})
                };
            }
        }
    }

    public class SubscriptionDefinition
    {
        public Uri Broker { get; set; }
        public Type MessageType { get; set; }
        public Func<object, Task> Handler { get; set; }
    }

    /// <summary>
    /// Marks a service as being capable of receiving messages.
    /// Follows convention that class has method with signature 'Task MethodName(MessageType message)'
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class SubscribeAttribute : Attribute
    {
        /// <summary>
        /// Type of message.
        /// </summary>
        public Type MessageType { get; }

        public SubscribeAttribute(Type messageType)
        {
            MessageType = messageType;
        }
    }
}