using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    /// <remarks>
    /// Base class for a <see cref="StatelessService"/> that serves as a subscriber of messages from the broker.
    /// Subscribe to message types and define a handler callback by using the <see cref="SubscribeAttribute"/>.
    /// </remarks>
    public abstract class SubscriberStatelessServiceBase : StatelessService, ISubscriberService
    {
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;

        /// <summary>
        /// When Set, this callback will be used to trace Service messages to.
        /// </summary>
        protected Action<string> ServiceEventSourceMessageCallback { get; set; }

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null)
            : base(serviceContext)
        {
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <summary>
        /// Subscribes to all message types that have a handler registered using the <see cref="SubscribeAttribute"/>.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override async Task OnOpenAsync(CancellationToken cancellationToken)
        {
            var serviceType = GetType();
            var subscriptions = _subscriberServiceHelper.DiscoverMessageHandlers(this);

            foreach (var subscription in subscriptions.Values)
            {
                try
                {
                    await _subscriberServiceHelper.RegisterMessageTypeAsync(this, subscription.MessageType, subscription.Broker);
                    ServiceEventSourceMessage($"Registered Service:'{serviceType.FullName}' Instance:'{Context.InstanceId}' as Subscriber of {subscription.MessageType}.");
                }
                catch (Exception ex)
                {
                    ServiceEventSourceMessage($"Failed to register Service:'{serviceType.FullName}' Instance:'{Context.InstanceId}' as Subscriber of {subscription.MessageType}. Error:'{ex}'.");
                }
            }
        }

        /// <summary>
        /// Receives a published message using the handler registered for the given type.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public Task ReceiveMessageAsync(MessageWrapper messageWrapper)
        {
            return _subscriberServiceHelper.ProccessMessageAsync(messageWrapper);
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
    }
}