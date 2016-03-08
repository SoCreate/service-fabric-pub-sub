using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.PublisherActors;

namespace ServiceFabric.PubSubActors.PublisherServices
{
    public static class PublisherServiceExtensions
    {
		/// <summary>
		/// Publish a message.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="message"></param>
		/// <param name="applicationName">The name of the SF application that hosts the <see cref="BrokerActor"/>. If not provided, ServiceInitializationParameters.CodePackageActivationContext.ApplicationName will be used.</param>
		/// <returns></returns>
		public static async Task PublishMessageAsync(this StatelessServiceBase service, object message, string applicationName = null)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (message == null) throw new ArgumentNullException(nameof(message));

			if (string.IsNullOrWhiteSpace(applicationName))
			{
				applicationName = service.ServiceInitializationParameters.CodePackageActivationContext.ApplicationName;
			}

			var brokerActor = GetBrokerActorForMessage(applicationName, message);
			var wrapper = PublisherActorExtensions.CreateMessageWrapper(message);
			await brokerActor.PublishMessageAsync(wrapper);
		}

		/// <summary>
		/// Publish a message.
		/// </summary>
		/// <param name="service"></param>
		/// <param name="message"></param>
		/// <param name="applicationName">The name of the SF application that hosts the <see cref="BrokerActor"/>. If not provided, ServiceInitializationParameters.CodePackageActivationContext.ApplicationName will be used.</param>
		/// <returns></returns>
		public static async Task PublishMessageAsync(this StatefulServiceBase service, object message, string applicationName = null)
		{
			if (service == null) throw new ArgumentNullException(nameof(service));
			if (message == null) throw new ArgumentNullException(nameof(message));

			if (string.IsNullOrWhiteSpace(applicationName))
			{
				applicationName = service.ServiceInitializationParameters.CodePackageActivationContext.ApplicationName;
			}

			var brokerActor = GetBrokerActorForMessage(applicationName, message);
			var wrapper = PublisherActorExtensions.CreateMessageWrapper(message);
			await brokerActor.PublishMessageAsync(wrapper);
		}

		/// <summary>
		/// Gets the <see cref="BrokerActor"/> instance for the provided <paramref name="message"/>
		/// </summary>
		/// <param name="applicationName"></param>
		/// <param name="message"></param>
		/// <returns></returns>
		private static IBrokerActor GetBrokerActorForMessage(string applicationName, object message)
		{
			ActorId actorId = new ActorId(message.GetType().FullName);
			IBrokerActor brokerActor = ActorProxy.Create<IBrokerActor>(actorId, applicationName, nameof(IBrokerActor));
			return brokerActor;
		}
	}
}
