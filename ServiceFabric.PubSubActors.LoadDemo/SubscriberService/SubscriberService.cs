using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using Microsoft.ServiceFabric.Services.Runtime;
using Newtonsoft.Json;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace SubscriberService
{
    /// <summary>
    /// An instance of this class is created for each service instance by the Service Fabric runtime.
    /// </summary>
    internal sealed class SubscriberService : StatelessService, ISubscriberService
    {
        private Dictionary<string, HashSet<Guid>> _messagesReceived;
        private readonly object _lockMe = new object();

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

            _messagesReceived = new Dictionary<string, HashSet<Guid>>();
            for (int i = 0; i < messageTypeCount; i++)
            {
                string messageTypeName = $"DataContract{i}";
                _messagesReceived[messageTypeName] = new HashSet<Guid>();
            }

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
                }
                catch (OperationCanceledException ex)
                {
                   
                }
            }

            ServiceEventSource.Current.ServiceMessage(this, $"Instance {Context.InstanceId} stopping. Total counts:{string.Join(", ", _messagesReceived.Select(m => $"Message Type '{m.Key}' - {m.Value.Count}"))}.");

        }

        public Task ReceiveMessageAsync(MessageWrapper message)
        {
            
            DataContract dc = JsonConvert.DeserializeObject<DataContract>(message.Payload);
            var set = _messagesReceived[message.MessageType];

            lock (_lockMe)
            {
                if (!set.Add(dc.Id))
                {
                    ServiceEventSource.Current.ServiceMessage(this, $"Received duplicate Message ID {dc.Id}.");
                }
                ServiceEventSource.Current.ServiceMessage(this, $"Instance {Context.InstanceId} Received Message Type {message.MessageType}. Total count:{set.Count}.");
            }
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
