using System;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
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
    public abstract class SubscriberStatelessServiceBase : StatelessService, ISubscriberService, ISubscriber
    {
        private readonly IBrokerClient _brokerClient;

        /// <summary>
        /// When Set, this callback will be used to log messages to.
        /// </summary>
        protected Action<string> Logger { get; set; }

        /// <summary>
        /// Set the Listener name so the remote Broker can find this service when there are multiple listeners available.
        /// </summary>
        protected string ListenerName { get; set; }

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext, IBrokerClient brokerClient = null)
            : base(serviceContext)
        {
            _brokerClient = brokerClient ?? new BrokerClient();
        }

        /// <inheritdoc/>
        protected override Task OnOpenAsync(CancellationToken cancellationToken)
        {
            return Subscribe();
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
            foreach (var handler in this.DiscoverMessageHandlers())
            {
                try
                {
                    await _brokerClient.SubscribeAsync(this, handler.Key, handler.Value, ListenerName);
                    LogMessage($"Registered Service:'{Context.ServiceName}' as Subscriber of {handler.Key}.");
                }
                catch (Exception ex)
                {
                    LogMessage($"Failed to register Service:'{Context.ServiceName}' as Subscriber of {handler.Key}. Error:'{ex.Message}'.");
                }
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
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