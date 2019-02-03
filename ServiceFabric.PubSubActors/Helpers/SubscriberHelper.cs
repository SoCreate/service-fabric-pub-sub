using System;
using System.Fabric;
using System.Threading.Tasks;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class SubscriberHelper : ISubscriberHelper
    {
        private readonly IBrokerServiceLocator _brokerServiceLocator;
        private readonly ServiceContext _context;
        private readonly Lazy<ServicePartitionInformation> _partitionInformation;

        public SubscriberHelper(ServiceContext context, Lazy<ServicePartitionInformation> partition, IBrokerServiceLocator locator = null):this(locator)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _partitionInformation = partition ?? throw new ArgumentNullException(nameof(partition));
        }

        protected SubscriberHelper(IBrokerServiceLocator brokerServiceLocator)
        {
            _brokerServiceLocator = brokerServiceLocator ?? new BrokerServiceLocator();
        }

        /// <summary>
        /// Registers this service as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task RegisterMessageTypeAsync(Type messageType, bool flushQueue = false, Uri brokerServiceName = null, string listenerName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(_context, _partitionInformation.Value, listenerName);
            await brokerService.RegisterServiceSubscriberAsync(serviceReference, messageType.FullName);
        }

        public Task RegisterMessageTypeAsync(Type messageType, Uri brokerServiceName = null, string listenerName = null)
        {
            return RegisterMessageTypeAsync(messageType, false, brokerServiceName, listenerName);
        }

        public Task UnregisterMessageTypeAsync(Type messageType, Uri brokerServiceName = null)
        {
            return UnregisterMessageTypeAsync(messageType, false, brokerServiceName);
        }

        /// <summary>
        /// Unregisters this service as a subscriber for messages of type <paramref name="messageType"/> with the <see cref="BrokerService"/>.
        /// </summary>
        /// <returns></returns>
        public async Task UnregisterMessageTypeAsync(Type messageType, bool flushQueue = false, Uri brokerServiceName = null)
        {
            if (messageType == null) throw new ArgumentNullException(nameof(messageType));
            if (brokerServiceName == null)
            {
                brokerServiceName = await PublisherServiceHelper.DiscoverBrokerServiceNameAsync();
                if (brokerServiceName == null)
                {
                    throw new InvalidOperationException("No brokerServiceName was provided or discovered in the current application.");
                }
            }
            var brokerService = await _brokerServiceLocator.GetBrokerServiceForMessageAsync(messageType.Name, brokerServiceName);
            var serviceReference = CreateServiceReference(_context, _partitionInformation.Value);
            await brokerService.UnregisterServiceSubscriberAsync(serviceReference, messageType.FullName, flushQueue);
        }

        /// <summary>
        /// Creates a <see cref="ServiceReference"/> for the provided service context and partition info.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="info"></param>
        /// <param name="listenerName">(optional) The name of the listener that is used to communicate with the service</param>
        /// <returns></returns>
        private static ServiceReference CreateServiceReference(ServiceContext context, ServicePartitionInformation info, string listenerName = null)
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