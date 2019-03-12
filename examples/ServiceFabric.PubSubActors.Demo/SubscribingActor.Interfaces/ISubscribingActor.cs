using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors;

namespace SubscribingActor.Interfaces
{
	public interface ISubscribingActor : ISubscriberActor
	{

        /// <summary>
        /// Registers this Actor as a subscriber for messages, with the <see cref="IBrokerActor"/>.
        /// </summary>
        /// <returns></returns>
        Task RegisterAsync();

		/// <summary>
		/// Unregisters this Actor as a subscriber for messages, with the <see cref="IBrokerActor"/>.
		/// </summary>
		/// <returns></returns>
		Task UnregisterAsync();



		/// <summary>
		/// Registers this Actor as a subscriber for messages using a relay broker, with the <see cref="IRelayBrokerActor"/>.
		/// </summary>
		/// <returns></returns>
		Task RegisterWithRelayAsync();

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages, with the <see cref="IRelayBrokerActor"/>.
        /// </summary>
        /// <returns></returns>
        Task UnregisterWithRelayAsync();


        /// <summary>
		/// Registers this Actor as a subscriber for messages, with the <see cref="IBrokerService"/>.
		/// </summary>
		/// <returns></returns>
		Task RegisterWithBrokerServiceAsync();

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages, with the <see cref="IBrokerService"/>.
        /// </summary>
        /// <returns></returns>
        Task UnregisterWithBrokerServiceAsync();
    }
}
