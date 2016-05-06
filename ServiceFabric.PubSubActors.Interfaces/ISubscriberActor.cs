using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Defines a common interface for all Subscriber Actors. 
	/// Don't forget to mark implementing <see cref="Actor"/> classes with
	/// the attribute <see cref="ActorServiceAttribute"/> like: [ActorService(Name = nameof(ISubscribingActor))] where ISubscribingActor is defined in your own project.
	/// </summary>
	public interface ISubscriberActor : IActor
	{
		/// <summary>
		/// Receives a published message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		Task ReceiveMessageAsync(MessageWrapper message);
	}
}