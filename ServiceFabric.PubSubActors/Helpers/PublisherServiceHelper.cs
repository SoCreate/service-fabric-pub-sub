using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.Helpers
{
    public  class PublisherServiceHelper : IPublisherServiceHelper
    {
        private readonly IBrokerServiceLocator _brokerServiceLocator;

        public PublisherServiceHelper()
        {
            _brokerServiceLocator = new BrokerServiceLocator();
        }

        public PublisherServiceHelper(IBrokerServiceLocator brokerServiceLocator)
        {
            _brokerServiceLocator = brokerServiceLocator;
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
        public  async Task PublishMessageAsync(StatelessService service, object message,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null)
            {
                brokerServiceName = await DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }

            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(message, brokerServiceName);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            await brokerService.PublishMessageAsync(wrapper);
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
        public  async Task PublishMessageAsync(StatefulServiceBase service, object message,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null)
            {
                brokerServiceName = await DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }

            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(message, brokerServiceName);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            await brokerService.PublishMessageAsync(wrapper);
        }

        /// <summary>
        /// Attempts to discover the <see cref="BrokerService"/> running in this Application.
        /// </summary>
        /// <returns></returns>
        public static Task<Uri> DiscoverBrokerServiceNameAsync()
        {
            var locator = new BrokerServiceLocator();
            return locator.LocateAsync();
        }
    }
}
