using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Subscriber
{
    /// <remarks>
    /// Base class for a <see cref="StatelessService"/> that serves as a subscriber of messages from the broker.
    /// Subscribe to message types and define a handler callback by using the <see cref="SubscribeAttribute"/>.
    /// </remarks>
    public abstract class SubscriberStatelessServiceBase : StatelessService, ISubscriberService
    {
        private readonly IBrokerClient _brokerClient;

        /// <summary>
        /// When Set, this callback will be used to log messages to.
        /// </summary>
        protected Action<string> Logger { get; set; }

        /// <summary>
        /// Set the Listener name so the remote Broker can find this service when there are multiple listeners available.
        /// </summary>
        protected string ListenerName { get; set; } = "SubscriberStatelessServiceRemotingListener";

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext, IBrokerClient brokerClient = null)
            : base(serviceContext)
        {
            _brokerClient = brokerClient ?? new BrokerClient();
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(CancellationToken cancellationToken)
        {
            for (var attempt = 0; ; attempt++)
            {
                try
                {
                    await Subscribe();
                    break;
                }
                catch (BrokerNotFoundException)
                {
                    if (attempt > 10)
                    {
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(10), cancellationToken);
                }
            }
        }

        /// <summary>
        /// Receives a published message using the handler registered for the given type.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public virtual Task ReceiveMessageAsync(MessageWrapper messageWrapper)
        {
            return _brokerClient.ProcessMessageAsync(messageWrapper);
        }

        /// <summary>
        /// Subscribe to all message types that have a handler method marked with a <see cref="SubscribeAttribute"/>.
        /// This method can be overriden to subscribe manually based on custom logic.
        /// </summary>
        /// <returns></returns>
        protected virtual async Task Subscribe()
        {
            foreach (var subscription in this.DiscoverSubscribeAttributes())
            {
                var subscribeAttribute = subscription.Value;
                
                try
                {
                    await _brokerClient.SubscribeAsync(
                        this,
                        subscription.Key,
                        subscribeAttribute.Handler,
                        ListenerName,
                        subscribeAttribute.RoutingKeyName,
                        subscribeAttribute.RoutingKeyValue,
                        subscribeAttribute.QueueType == QueueType.Ordered);
                    LogMessage($"Registered Service:'{Context.ServiceName}' as Subscriber of {subscription.Key}.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to register Service:'{Context.ServiceName}' as Subscriber of {subscription.Key}. Error:'{ex.Message}'.");
                    throw;
                }
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            yield return new ServiceInstanceListener(context => new FabricTransportServiceRemotingListener(context, this), ListenerName);
        }

        /// <summary>
        /// Outputs the provided message to the <see cref="Logger"/> if it's configured.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        protected void LogMessage(string message, [CallerMemberName] string caller = "unknown")
        {
            Logger?.Invoke($"{caller} - {message}");
        }
    }
}