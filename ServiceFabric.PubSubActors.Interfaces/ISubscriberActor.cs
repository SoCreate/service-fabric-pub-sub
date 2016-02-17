using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Defines a common interface for all Subscriber Actors. 
	/// Don't forget to mark implementing <see cref="ActorBase"/> classes with
	/// the attribute <see cref="ActorServiceAttribute"/> like: [ActorService(Name = nameof(ISubscribingActor))] where ISubscribingActor is defined in your own project.
	/// </summary>
	public interface ISubscriberActor : IActor
	{
		/// <summary>
		/// Registers this Actor as a subscriber for messages.
		/// </summary>
		/// <returns></returns>
		Task RegisterAsync();

		/// <summary>
		/// Receives a published message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		Task ReceiveMessageAsync(MessageWrapper message);
	}
}