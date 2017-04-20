using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
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
        public static async Task PublishMessageAsync(this StatelessService service, object message,
            string applicationName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(applicationName))
            {
                applicationName = service.Context.CodePackageActivationContext.ApplicationName;
            }

            var brokerActor = GetBrokerActorForMessage(applicationName, message);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            await brokerActor.PublishMessageAsync(wrapper);
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="applicationName">The name of the SF application that hosts the <see cref="BrokerActor"/>. If not provided, ServiceInitializationParameters.CodePackageActivationContext.ApplicationName will be used.</param>
        /// <returns></returns>
        public static async Task PublishMessageAsync(this StatefulServiceBase service, object message,
            string applicationName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));

            if (string.IsNullOrWhiteSpace(applicationName))
            {
                applicationName = service.Context.CodePackageActivationContext.ApplicationName;
            }

            var brokerActor = GetBrokerActorForMessage(applicationName, message);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
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


        ///////broker service code


        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.PublisherServiceHelper for testability")]
        public static async Task PublishMessageToBrokerServiceAsync(this StatelessService service, object message,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null)
            {
                brokerServiceName =
                    await
                        DiscoverBrokerServiceNameAsync(
                            new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }

            var brokerService =
                await PublisherActorExtensions.GetBrokerServiceForMessageAsync(message, brokerServiceName);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            await brokerService.PublishMessageAsync(wrapper);
        }

        /// <summary>
        /// Publish a message.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">The name of a SF Service of type <see cref="BrokerService"/>.</param>
        /// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.PublisherServiceHelper for testability")]
        public static async Task PublishMessageToBrokerServiceAsync(this StatefulServiceBase service, object message,
            Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null)
            {
                brokerServiceName =
                    await
                        DiscoverBrokerServiceNameAsync(
                            new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException(
                        "No brokerServiceName was provided or discovered in the current application.");
                }
            }

            var brokerService =
                await PublisherActorExtensions.GetBrokerServiceForMessageAsync(message, brokerServiceName);
            var wrapper = MessageWrapper.CreateMessageWrapper(message);
            await brokerService.PublishMessageAsync(wrapper);
        }

        /// <summary>
        /// Attempts to discover the <see cref="BrokerService"/> running in this Application.
        /// </summary>
        /// <param name="applicationName"></param>
        /// <returns></returns>
		[Obsolete("Use ServiceFabric.PubSubActors.Helpers.BrokerServiceLocator for testability")]

        public static async Task<Uri> DiscoverBrokerServiceNameAsync(Uri applicationName)
        {
            try
            {
                var fc = new System.Fabric.FabricClient();
                var property = await fc.PropertyManager.GetPropertyAsync(applicationName, nameof(BrokerService));
                if (property == null) return null;
                string value = property.GetValue<string>();
                return new Uri(value);
            }
            // ReSharper disable once EmptyGeneralCatchClause
            catch
            {
            }
            return null;
        }
    }
}
