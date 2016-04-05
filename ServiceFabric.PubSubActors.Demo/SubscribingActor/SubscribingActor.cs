using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using SubscribingActor.Interfaces;
using ServiceFabric.PubSubActors.SubscriberActors;

namespace SubscribingActor
{
	/// <remarks>
	/// Each ActorID maps to an instance of this class.
	/// The ISubscribingActor interface (in a separate DLL that client code can
	/// reference) defines the operations exposed by SubscribingActor objects.
	/// </remarks>
	[ActorService(Name = nameof(ISubscribingActor))]
	[StatePersistence(StatePersistence.None)]
	internal class SubscribingActor : Actor, ISubscribingActor
	{
		public Task RegisterAsync()
		{
			return this.RegisterMessageTypeAsync(typeof(PublishedMessageOne)); //register as subscriber for this type of messages
		}

		public Task ReceiveMessageAsync(MessageWrapper message)
		{
			var payload = this.Deserialize<PublishedMessageOne>(message);
			ActorEventSource.Current.ActorMessage(this, $"Received message: {payload.Content}");
			//TODO: handle message
			return Task.FromResult(true);
		}



		Task<string> ISubscribingActor.DoWorkAsync()
		{
			// TODO: Replace the following with your own logic.
			ActorEventSource.Current.ActorMessage(this, "Doing Work");
			return Task.FromResult("Work result");
		}
	}
}
