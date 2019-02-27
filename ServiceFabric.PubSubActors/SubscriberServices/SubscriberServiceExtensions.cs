using Microsoft.ServiceFabric.Services.Runtime;
using ServiceFabric.PubSubActors.Interfaces;
using System;
using System.Fabric;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
    public static class SubscriberServiceExtensions
    {
        /// <summary>
        /// Deserializes the provided <paramref name="messageWrapper"/> Payload into an instance of type <typeparam name="TResult"></typeparam>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(this StatefulServiceBase service, MessageWrapper messageWrapper)
        {
            if (messageWrapper == null) throw new ArgumentNullException(nameof(messageWrapper));
            if (string.IsNullOrWhiteSpace(messageWrapper.Payload)) throw new ArgumentNullException(nameof(messageWrapper.Payload));

            var payload = messageWrapper.CreateMessage<TResult>();
            return payload;
        }

        /// <summary>
        /// Deserializes the provided <paramref name="messageWrapper"/> Payload into an instance of type <typeparam name="TResult"></typeparam>
        /// </summary>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult Deserialize<TResult>(this StatelessService service, MessageWrapper messageWrapper)
        {
            if (messageWrapper == null) throw new ArgumentNullException(nameof(messageWrapper));
            if (string.IsNullOrWhiteSpace(messageWrapper.Payload)) throw new ArgumentNullException(nameof(messageWrapper.Payload));

            var payload = messageWrapper.CreateMessage<TResult>();
            return payload;
        }

        /// <summary>
        /// Registers this Service as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public static async Task RegisterMessageTypeWithBrokerServiceAsync(this StatelessService service, Type messageType, Uri brokerServiceName = null, string listenerName = null, string routingKey = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service.Context, service.GetServicePartition().PartitionInfo, listenerName);
            await brokerService.RegisterServiceSubscriberAsync(serviceReference, messageType.FullName, routingKey);
        }

        /// <summary>
        /// Unregisters this Service as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public static async Task UnregisterMessageTypeWithBrokerServiceAsync(this StatelessService service, Type messageType, bool flushQueue, Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service.Context, service.GetServicePartition().PartitionInfo);
            await brokerService.UnregisterServiceSubscriberAsync(serviceReference, messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Registers this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public static async Task RegisterMessageTypeWithBrokerServiceAsync(this StatefulService service, Type messageType, Uri brokerServiceName = null, string listenerName = null, string routingKey = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service.Context, service.GetServicePartition().PartitionInfo, listenerName);
            await brokerService.RegisterServiceSubscriberAsync(serviceReference, messageType.FullName, routingKey);
        }

        /// <summary>
        /// Unregisters this Actor as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public static async Task UnregisterMessageTypeWithBrokerServiceAsync(this StatefulService service, Type messageType, bool flushQueue, Uri brokerServiceName = null)
        {
            if (service == null) throw new ArgumentNullException(nameof(service));
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServices.PublisherServiceExtensions.DiscoverBrokerServiceNameAsync(new Uri(service.Context.CodePackageActivationContext.ApplicationName));
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await PublisherActors.PublisherActorExtensions.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(service.Context, service.GetServicePartition().PartitionInfo);
            await brokerService.UnregisterServiceSubscriberAsync(serviceReference, messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Gets the Partition info for the provided StatefulServiceBase instance.
        /// </summary>
        /// <param name="serviceBase"></param>
        /// <returns></returns>
        private static IStatefulServicePartition GetServicePartition(this StatefulServiceBase serviceBase)
        {
            if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
            return (IStatefulServicePartition)serviceBase
                .GetType()
                .GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(serviceBase);
        }

        /// <summary>
        /// Gets the Partition info for the provided StatelessService instance.
        /// </summary>
        /// <param name="serviceBase"></param>
        /// <returns></returns>
        private static IStatelessServicePartition GetServicePartition(this StatelessService serviceBase)
        {
            if (serviceBase == null) throw new ArgumentNullException(nameof(serviceBase));
            return (IStatelessServicePartition)serviceBase
                .GetType()
                .GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(serviceBase);
        }

        /// <summary>
        /// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        /// <param name="listenerName">(optional) The name of the listener that is used to communicate with the service</param>
        /// <returns></returns>
        public static ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info, string listenerName = null)
        {
            var serviceReference = new ServiceReference
            {
                ApplicationName = context.CodePackageActivationContext.ApplicationName,
                PartitionKind = info.Kind,
                ServiceUri = context.ServiceName,
                PartitionGuid = context.PartitionId,
                ListenerName = listenerName
            };

            if (info is Int64RangePartitionInformation longInfo)
            {
                serviceReference.PartitionKey = longInfo.LowKey;
            }
            else if (info is NamedPartitionInformation stringInfo)
            {
                serviceReference.PartitionName = stringInfo.Name;
            }

            return serviceReference;
        }
    }
}
