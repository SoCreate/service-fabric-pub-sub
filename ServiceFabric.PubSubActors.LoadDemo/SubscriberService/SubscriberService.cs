using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscriberService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class SubscriberService : StatelessService, ISubscriberService
    {
        private int _messagesReceived; 

        public SubscriberService(StatelessServiceContext context)
            : base(context)
        { }

        /// <summary>
        /// Optional override to create listeners 
        /// </summary>
        /// <returns>A collection of listeners.</returns>
        protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
        {
            //Pub-sub listener:
            yield return new ServiceInstanceListener(p => new SubscriberCommunicationListener(this, p), "StatelessSubscriberCommunicationListener");
        }


        /// <summary>
        /// This is the main entry point for your service instance.
        /// </summary>
        /// <param name="cancellationToken">Canceled when Service Fabric needs to shut down this service instance.</param>
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            var brokerServiceName = await ServiceFabric.PubSubActors.PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(Context.CodePackageActivationContext.ApplicationName));

            //subscribe to messages by their type name:
            int messageTypeCount;
            string setting = GetConfigurationValue(Context, "MessageSettings", "MessageTypeCount");
            if (string.IsNullOrWhiteSpace(setting) || !int.TryParse(setting, out messageTypeCount))
            {
                return;
            }

            for (int i = 0; i < messageTypeCount; i++)
            {
                string messageTypeName = $"DataContract{i}";
                var brokerService = await ServiceFabric.PubSubActors.PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageTypeName, brokerServiceName);
                var serviceReference = SubscriberServiceExtensions.CreateServiceReference(Context, Partition.PartitionInfo);
                await brokerService.RegisterServiceSubscriberAsync(serviceReference, messageTypeName);

                ServiceEventSource.Current.ServiceMessage(this, $"Subscribing to Message Type {messageTypeName}.");
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
            }
        }

        public Task ReceiveMessageAsync(MessageWrapper message)
        {
            int count = Interlocked.Increment(ref _messagesReceived);
            ServiceEventSource.Current.ServiceMessage(this, $"Received Message Type {message.MessageType}. Total count:{count}.");

            return Task.FromResult(true);
        }

        private static string GetConfigurationValue(ServiceContext context, string sectionName, string parameterName)
        {
            var configSection = context.CodePackageActivationContext.GetConfigurationPackageObject("Config");
            var section = (configSection?.Settings.Sections.Contains(sectionName) ?? false) ? configSection?.Settings.Sections[sectionName] : null;
            string endPointType = (section?.Parameters.Contains(parameterName) ?? false) ? section.Parameters[parameterName].Value : null;
            return endPointType;
        }
    }
}
