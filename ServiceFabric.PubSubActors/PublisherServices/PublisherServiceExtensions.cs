using System;
using System.Threading.Tasks;
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
            var wrapper = message.CreateMessageWrapper();
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
            var wrapper = message.CreateMessageWrapper();
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
