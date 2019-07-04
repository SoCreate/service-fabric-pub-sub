using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.PubSub.Helpers
{
    public interface IBrokerServiceLocator
    {
        /// <summary>
        /// Registers the default <see cref="BrokerService"/> by name.
        /// </summary>
        /// <returns></returns>
        Task RegisterAsync();

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="message"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task<IBrokerService> GetBrokerServiceForMessageAsync(object message);

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="messageTypeName"/>
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <returns></returns>
        Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName);

        /// <summary>
        /// Gets a collection of <see cref="IBrokerService"/> ServiceProxy instances, one for each partition.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<IBrokerService>> GetBrokerServicesForAllPartitionsAsync();
    }
}