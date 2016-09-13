using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.Helpers
{
    /// <summary>
	/// Common operations of <see cref="ServiceFabric.PubSubActors.PublisherActors"/>
	/// </summary>
	public class PublisherActorHelper : IPublisherActorHelper
    {
        private readonly IBrokerServiceLocator _brokerServiceLocator;

        public PublisherActorHelper()
        {
            _brokerServiceLocator = new BrokerServiceLocator();
        }

        public PublisherActorHelper(IBrokerServiceLocator brokerServiceLocator)
        {
            _brokerServiceLocator = brokerServiceLocator;
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="actor"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of the SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
        public async Task PublishMessageAsync(ActorBase actor, object message, Uri brokerServiceName = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(message, brokerServiceName);            
            await brokerService.PublishMessageAsync(wrapper);
        }
    }
}
