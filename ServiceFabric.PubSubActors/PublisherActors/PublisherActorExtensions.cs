using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.PublisherActors
{
	/// <summary>
	/// Common operations of <see cref="ServiceFabric.PubSubActors.PublisherActors"/>
	/// </summary>
	public static class PublisherActorExtensions
	{
		/// <summary>
		/// Publish a message.
		/// </summary>
		/// <param name="actor"></param>
		/// <param name="message"></param>
		/// <param name="applicationName">The name of the SF application that hosts the <see cref="BrokerActor"/>. If not provided, actor.ApplicationName will be used.</param>
		/// <returns></returns>
		public static async Task PublishMessageAsync(this ActorBase actor, object message, string applicationName = null)
		{
			if (actor == null) throw new ArgumentNullException(nameof(actor));
			if (message == null) throw new ArgumentNullException(nameof(message));

			if (string.IsNullOrWhiteSpace(applicationName))
			{
				applicationName = actor.ApplicationName;
			}

			var brokerActor = GetBrokerActorForMessage(message, applicationName);
			var wrapper = CreateMessageWrapper(message);
			await brokerActor.PublishMessageAsync(wrapper);
		}

		/// <summary>
		/// Convert the provided <paramref name="message"/> into a <see cref="MessageWrapper"/>
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		internal static MessageWrapper CreateMessageWrapper(object message)
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
		/// <param name="message"></param>
		/// <param name="applicationName"></param>
		/// <returns></returns>
		private static IBrokerActor GetBrokerActorForMessage(object message, string applicationName)
		{
			ActorId actorId = new ActorId(message.GetType().FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, applicationName, nameof(IBrokerActor));
			return brokerActor;
		}
	}
}
