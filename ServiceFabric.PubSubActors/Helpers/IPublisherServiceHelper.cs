using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface IPublisherServiceHelper
    {
        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
        Task PublishMessageAsync(StatelessService service, object message,
            Uri brokerServiceName = null);

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
        Task PublishMessageAsync(StatefulServiceBase service, object message,
            Uri brokerServiceName = null);
    }
}