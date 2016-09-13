using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors
{
	/// <summary>
	/// Special implementation of <see cref="BrokerActor"/> that receives and forwards incoming messages.
	/// </summary>
	[StatePersistence(StatePersistence.Persisted)]
	public class RelayBrokerActor : BrokerActor, IRelayBrokerActor
	{
        /// <summary>
        /// Initializes a new instance of <see cref="RelayBrokerActor" />
        /// </summary>
        /// <param name="actorService">
        /// The <see cref="Microsoft.ServiceFabric.Actors.Runtime.ActorService" /> that will host this actor instance.
        /// </param>
        /// <param name="actorId">
        /// The <see cref="Microsoft.ServiceFabric.Actors.ActorId" /> for this actor instance.
        /// </param>
        public RelayBrokerActor(ActorService actorService, ActorId actorId) 
            : base(actorService, actorId)
	    {
	    }

	    /// <summary>
		/// Publishes the received message to all registered subscribers.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		public Task ReceiveMessageAsync(MessageWrapper message)
		{
			message.IsRelayed = true;
			return PublishMessageAsync(message);
		}
	}
}
