using System;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Remoting.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
using System.Collections.Generic;
using System.Fabric;
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
        /// Meta data about the service that helps the Broker deliver messages to this service.
        /// </summary>
        protected ServiceReference ServiceReference;

        /// <summary>
        /// The message types that this service subscribes to and their respective handler methods.
        /// </summary>
        protected Dictionary<Type, Func<object, Task>> Handlers { get; set; } = new Dictionary<Type, Func<object, Task>>();

        /// <summary>
        /// Set the Listener name so the remote Broker can find this service when there are multiple listeners available.
        /// </summary>
        protected string ListenerName { get; set; }

        protected SubscriberStatelessServiceBase(StatelessServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null)
            : base(serviceContext)
        {
            _subscriberServiceHelper = subscriberServiceHelper ?? new SubscriberServiceHelper(new BrokerServiceLocator());
        }

        /// <summary>
        /// Subscribes to all message types that have a handler registered using the <see cref="SubscribeAttribute"/>.
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected override Task OnOpenAsync(CancellationToken cancellationToken)
        {
            Handlers = _subscriberServiceHelper.DiscoverMessageHandlers(this);
            ServiceReference = _subscriberServiceHelper.CreateServiceReference(this, ListenerName);
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
            return _subscriberServiceHelper.SubscribeAsync(ServiceReference, Handlers.Keys);
        }

        /// <inheritdoc/>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            return this.CreateServiceRemotingInstanceListeners();
        }
    }
}