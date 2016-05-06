using System.Threading.Tasks;
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
