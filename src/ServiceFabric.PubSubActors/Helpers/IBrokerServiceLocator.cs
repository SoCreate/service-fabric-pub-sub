using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface IBrokerServiceLocator
    {
        /// <summary>
        /// Locates the registered broker service.
        /// </summary>
        /// <returns></returns>
        Task<Uri> LocateAsync();

        /// <summary>
        /// Registers the default <see cref="BrokerServiceBase"/> by name.
        /// </summary>
        /// <param name="brokerServiceName"></param>
        /// <returns></returns>
        Task RegisterAsync(Uri brokerServiceName);

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="message"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">Uri of BrokerService instance</param>
        /// <returns></returns>
        Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName = null);

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="messageTypeName"/>
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="brokerServiceName">Uri of BrokerService instance</param>
        /// <returns></returns>
        Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName = null);

        /// <summary>
        /// Gets a collection of <see cref="IBrokerService"/> ServiceProxy instances, one for each partition.
        /// </summary>
        /// <param name="brokerServiceName"></param>
        /// <returns></returns>
        Task<IEnumerable<IBrokerService>> GetBrokerServicesForAllPartitionsAsync(Uri brokerServiceName = null);
    }
}