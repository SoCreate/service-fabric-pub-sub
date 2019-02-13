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
            return _subscriberServiceHelper.SubscribeAsync(this, _subscriberServiceHelper.CreateServiceReference(this));
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
    }
}