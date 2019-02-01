using System;
using System.Fabric;
using System.Fabric.Query;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class BrokerServiceLocator : IBrokerServiceLocator
    {
        private static ServicePartitionList _cachedPartitions;
        private readonly FabricClient _fabricClient;

        /// <summary>
        /// Creates a new default instance.
        /// </summary>
        public BrokerServiceLocator()
        {
            _fabricClient = new FabricClient();
        }


        /// <inheritdoc />
        public async Task RegisterAsync(Uri brokerServiceName)
        {
            var activationContext = FabricRuntime.GetActivationContext();
            await _fabricClient.PropertyManager.PutPropertyAsync(new Uri(activationContext.ApplicationName), nameof(BrokerService), brokerServiceName.ToString());
        }

        /// <inheritdoc />
        public async Task<Uri> LocateAsync()
        {
            try
            {
                // check current context
                var activationContext = FabricRuntime.GetActivationContext();
                var property = await GetBrokerProperty(activationContext.ApplicationName);
                
                if (property == null)
                {
                    // try to find borker name in other application types
                    var apps = await _fabricClient.QueryManager.GetApplicationListAsync();

                    foreach (var app in apps)
                    {
                        var found = await LocateAsync(app.ApplicationName);
                        if(found != null)
                            return found;
                    }
                }
                else
                {
                    return new Uri(property.GetValue<string>());
                }
            }
            catch
            {
                ;
            }
            return null;
        }

        private async Task<Uri> LocateAsync(Uri applicationName)
        {
            var property = await GetBrokerProperty(applicationName);

            return property != null ? new Uri(property.GetValue<string>()) : null;
        }
        private async Task<NamedProperty> GetBrokerProperty(string applicationName)
        {
           return await GetBrokerProperty(new Uri(applicationName));
        }
        private async Task<NamedProperty> GetBrokerProperty(Uri applicationName)
        {
           return await _fabricClient.PropertyManager.GetPropertyAsync(applicationName, nameof(BrokerService));
        }

        /// <inheritdoc />
        public async Task<ServicePartitionKey> GetPartitionForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            if (_cachedPartitions == null)
            {
                _cachedPartitions = await _fabricClient.QueryManager.GetPartitionListAsync(brokerServiceName);
            }
            int index = Math.Abs(messageTypeName.GetHashCode() % _cachedPartitions.Count);
            var partition = _cachedPartitions[index];
            if (partition.PartitionInformation.Kind != ServicePartitionKind.Int64Range)
            {
                throw new InvalidOperationException("Sorry, only Int64 Range Partitions are supported.");
            }

            var info = (Int64RangePartitionInformation)partition.PartitionInformation;
            var resolvedPartition = new ServicePartitionKey(info.LowKey);

            return resolvedPartition;
        }

        /// <inheritdoc />
        public Task<ServicePartitionKey> GetPartitionForMessageAsync(object message, Uri brokerServiceName)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null) throw new ArgumentNullException(nameof(brokerServiceName));

            string messageTypeName = (message.GetType().FullName);
            return GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(message, brokerServiceName);
            var brokerService = ServiceProxy.Create<IBrokerService>(brokerServiceName, resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
            return brokerService;
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
            var brokerService = ServiceProxy.Create<IBrokerService>(brokerServiceName, resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
            return brokerService;
        }
    }
}
