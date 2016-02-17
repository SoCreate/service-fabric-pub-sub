using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Common operations of <see cref="ServiceFabric.PubSubActors.PublisherActors"/>
	/// </summary>
	internal static class PublisherActorExtensions
	{
		/// <summary>
		/// Publish a message.
		/// </summary>
		/// <param name="actor"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static async Task CommonPublishMessageAsync(this ActorBase actor, object message)
		{
			var brokerActor = GetBrokerActorForMessage(actor, message);
			var wrapper = CreateMessageWrapper(actor, message);
			await brokerActor.PublishMessageAsync(wrapper);
		}

		/// <summary>
		/// Convert the provided <paramref name="message"/> into a <see cref="MessageWrapper"/>
		/// </summary>
		/// <param name="actor"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static MessageWrapper CreateMessageWrapper(this ActorBase actor, object message)
		{
			var wrapper = new MessageWrapper
			{
				MessageType = message.GetType().FullName,
				Payload = JsonConvert.SerializeObject(message),
			};
			return wrapper;
		}

		/// <summary>
		/// Gets the <see cref="BrokerActor"/> instance for the provided <paramref name="message"/>
		/// </summary>
		/// <param name="actor"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		public static IBrokerActor GetBrokerActorForMessage(this ActorBase actor, object message)
		{
			ActorId actorId = new ActorId(message.GetType().FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, actor.ApplicationName, nameof(IBrokerActor));
			return brokerActor;
		}
	}
}
