using System;
using System.Fabric;
using System.Fabric.Description;
using System.Fabric.Query;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class BrokerServiceLocator : IBrokerServiceLocator
    {
        private static ServicePartitionList _cachedPartitions;
        private readonly FabricClient _fabricClient;
        private const string _brokerName = nameof(BrokerService);
        private readonly IServiceProxyFactory _serviceProxyFactory;

        /// <summary>
        /// Creates a new default instance.
        /// </summary>
        public BrokerServiceLocator(bool useRemotingV2 = false)
        {
            _fabricClient = new FabricClient();

#if NETSTANDARD2_0

            _serviceProxyFactory = new ServiceProxyFactory(c => new FabricTransportServiceRemotingClientFactory());
#else
            if (useRemotingV2)
            {
                _serviceProxyFactory = new ServiceProxyFactory(c => new FabricTransportServiceRemotingClientFactory());
            }
            else
            {
                _serviceProxyFactory = new ServiceProxyFactory();
            }
#endif
        }


        /// <inheritdoc />
        public async Task RegisterAsync(Uri brokerServiceName)
        {
            var activationContext = FabricRuntime.GetActivationContext();
            await _fabricClient.PropertyManager.PutPropertyAsync(new Uri(activationContext.ApplicationName), _brokerName, brokerServiceName.ToString());
        }

        /// <inheritdoc />
        public async Task<Uri> LocateAsync()
        {
            try
            {
                // check current context
                var activationContext = FabricRuntime.GetActivationContext();
                var property = await GetBrokerPropertyOrNull(activationContext.ApplicationName);

                if (property == null)
                {
                    // try to find broker name in other application types
                    bool hasPages = true;
                    
                    var query = new ApplicationQueryDescription() { MaxResults = 50 };

                    while (hasPages)
                    {
                        var apps = await _fabricClient.QueryManager.GetApplicationPagedListAsync(query);

                        query.ContinuationToken = apps.ContinuationToken;

                        hasPages = !string.IsNullOrEmpty(query.ContinuationToken);

                        foreach (var app in apps)
                        {
                            var found = await LocateAsync(app.ApplicationName);
                            if (found != null)
                                return found;
                        }
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
            var property = await GetBrokerPropertyOrNull(applicationName);

            return property != null ? new Uri(property.GetValue<string>()) : null;
        }
        private async Task<NamedProperty> GetBrokerPropertyOrNull(string applicationName)
        {
            return await GetBrokerPropertyOrNull(new Uri(applicationName));
        }
        private async Task<NamedProperty> GetBrokerPropertyOrNull(Uri applicationName)
        {
            try
            {
                return await _fabricClient.PropertyManager.GetPropertyAsync(applicationName, _brokerName);
            }
            catch
            {
                ;
            }
            return null;
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

            string messageTypeName = message.GetType().FullName;
            return GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(message, brokerServiceName);
            var brokerService = _serviceProxyFactory.CreateServiceProxy<IBrokerService>(brokerServiceName, resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
            return brokerService;
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
            var brokerService = _serviceProxyFactory.CreateServiceProxy<IBrokerService>(brokerServiceName, resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
            return brokerService;
        }
    }
}
