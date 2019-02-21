using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    public class SubscriberStatefulServiceBase : StatefulService, ISubscriberService
    {
        private readonly ISubscriberServiceHelper _subscriberServiceHelper;

        /// <summary>
        /// The message types that this service subscribes to and their respective handler methods.
        /// </summary>
        protected Dictionary<Type, Func<object, Task>> Handlers { get; set; } = new Dictionary<Type, Func<object, Task>>();

        /// <summary>
        /// Set the Listener name so the remote Broker can find this service when there are multiple listeners available.
        /// </summary>
        protected string ListenerName { get; set; }

        /// <summary>
        /// Creates a new instance using the provided context.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="subscriberServiceHelper"></param>
        protected SubscriberStatefulServiceBase(StatefulServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null)
            : base(serviceContext)
        {
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <summary>
        /// Creates a new instance using the provided context.
        /// </summary>
        /// <param name="serviceContext"></param>
        /// <param name="reliableStateManagerReplica"></param>
        /// <param name="subscriberServiceHelper"></param>
        protected SubscriberStatefulServiceBase(StatefulServiceContext serviceContext,
            IReliableStateManagerReplica2 reliableStateManagerReplica, ISubscriberServiceHelper subscriberServiceHelper = null)
            : base(serviceContext, reliableStateManagerReplica)
        {
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <inheritdoc/>
        protected override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
        {
            Handlers = _subscriberServiceHelper.DiscoverMessageHandlers(this);
            return Subscribe();
        }

        /// <summary>
        /// Receives a published message using the handler registered for the given type.
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public virtual Task ReceiveMessageAsync(MessageWrapper messageWrapper)
        {
            return _subscriberServiceHelper.ProccessMessageAsync(messageWrapper, Handlers);
        }

        /// <summary>
        /// Subscribe to all message types that have a handler method marked with a <see cref="SubscribeAttribute"/>.
        /// This method can be overriden to subscribe manually based on custom logic.
        /// </summary>
        /// <returns></returns>
        protected virtual Task Subscribe()
        {
            var serviceReference = _subscriberServiceHelper.CreateServiceReference(this, ListenerName);
            return _subscriberServiceHelper.SubscribeAsync(serviceReference, Handlers.Keys);
        }

        /// <inheritdoc />
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            return this.CreateServiceRemotingReplicaListeners();
        }
    }
}