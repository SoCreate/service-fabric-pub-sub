using System;
using System.Collections.Generic;
using System.Fabric;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Subscriber
{
    public class SubscriberStatefulServiceBase : StatefulService, ISubscriberService
    {
        private readonly IBrokerClient _brokerClient;

        /// <summary>
        /// When Set, this callback will be used to log messages to.
        /// </summary>
        protected Action<string> Logger { get; set; }

        /// <summary>
        /// Set the Listener name so the remote Broker can find this service when there are multiple listeners available.
        /// </summary>
        protected string ListenerName { get; set; } = "SubscriberStatefulServiceRemotingListener";

        /// <summary>
        /// Creates a new instance using the provided context.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="brokerClient"></param>
        protected SubscriberStatefulServiceBase(StatefulServiceContext serviceContext, IBrokerClient brokerClient = null)
            : base(serviceContext)
        {
            _brokerClient = brokerClient ?? new BrokerClient();
        }

        /// <summary>
        /// Creates a new instance using the provided context.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="reliableStateManagerReplica"></param>
        /// <param name="brokerClient"></param>
        protected SubscriberStatefulServiceBase(StatefulServiceContext serviceContext,
            IReliableStateManagerReplica2 reliableStateManagerReplica, IBrokerClient brokerClient = null)
            : base(serviceContext, reliableStateManagerReplica)
        {
            _brokerClient = brokerClient ?? new BrokerClient();
        }

        /// <inheritdoc/>
        protected override async Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
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
                    throw;
                }
            }
        }

        /// <inheritdoc />
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            yield return new ServiceReplicaListener(context => new FabricTransportServiceRemotingListener(context, this), ListenerName);
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