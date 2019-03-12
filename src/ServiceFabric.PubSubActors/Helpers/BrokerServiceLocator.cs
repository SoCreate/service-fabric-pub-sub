using System;
using System.Collections.Generic;
using System.Fabric;
using System.Fabric.Description;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class BrokerServiceLocator : IBrokerServiceLocator
    {
        private readonly IHashingHelper _hashingHelper;
        private static List<ServicePartitionKey> _cachedPartitionKeys = new List<ServicePartitionKey>();
        private static Uri _cachedBrokerUri;
        private readonly FabricClient _fabricClient;
        private const string BrokerName = nameof(BrokerService);
        private readonly IServiceProxyFactory _serviceProxyFactory;

        /// <summary>
        /// Creates a new default instance.
        /// </summary>
        public BrokerServiceLocator(IHashingHelper hashingHelper = null)
        {
            _hashingHelper = hashingHelper ?? new HashingHelper();
            _fabricClient = new FabricClient();
            _serviceProxyFactory = new ServiceProxyFactory(c => new FabricTransportServiceRemotingClientFactory());
        }


        /// <inheritdoc />
        public async Task RegisterAsync(Uri brokerServiceName)
        {
            var activationContext = FabricRuntime.GetActivationContext();
            await _fabricClient.PropertyManager.PutPropertyAsync(new Uri(activationContext.ApplicationName), BrokerName, brokerServiceName.ToString());
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName = null)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            var resolvedPartition = await GetPartitionForMessageAsync(message, brokerServiceName);
            return _serviceProxyFactory.CreateServiceProxy<IBrokerService>(
                brokerServiceName ?? await LocateAsync(), resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
        }

        /// <inheritdoc />
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName = null)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
            return _serviceProxyFactory.CreateServiceProxy<IBrokerService>(
                brokerServiceName ?? await LocateAsync(), resolvedPartition, listenerName: BrokerServiceBase.ListenerName);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<IBrokerService>> GetBrokerServicesForAllPartitionsAsync(Uri brokerServiceName = null)
        {
            var serviceProxies = new List<IBrokerService>();
            foreach (var partition in await GetBrokerPartitionKeys(brokerServiceName))
            {
                serviceProxies.Add(_serviceProxyFactory.CreateServiceProxy<IBrokerService>(
                    brokerServiceName ?? await LocateAsync(), partition, listenerName: BrokerServiceBase.ListenerName));
            }

            return serviceProxies;
        }

        /// <inheritdoc />
        public async Task<Uri> LocateAsync()
        {
            if (_cachedBrokerUri != null)
            {
                return _cachedBrokerUri;
            }

            try
            {
                // check current context
                var activationContext = FabricRuntime.GetActivationContext();
                var property = await GetBrokerPropertyOrNull(activationContext.ApplicationName);

                if (property == null)
                {
                    // try to find broker name in other application types
                    bool hasPages = true;

                    var query = new ApplicationQueryDescription { MaxResults = 50 };

                    while (hasPages)
                    {
                        var apps = await _fabricClient.QueryManager.GetApplicationPagedListAsync(query);

                        query.ContinuationToken = apps.ContinuationToken;

                        hasPages = !string.IsNullOrEmpty(query.ContinuationToken);

                        foreach (var app in apps)
                        {
                            var found = await LocateAsync(app.ApplicationName);
                            if (found != null)
                            {
                                _cachedBrokerUri = found;
                                return _cachedBrokerUri;
                            }
                        }
                    }
                }
                else
                {
                    _cachedBrokerUri = new Uri(property.GetValue<string>());
                    return _cachedBrokerUri;
                }
            }
            catch
            {
                ;
            }
            throw new InvalidOperationException("No brokerService was discovered in the cluster.");
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
                return await _fabricClient.PropertyManager.GetPropertyAsync(applicationName, BrokerName);
            }
            catch
            {
                ;
            }
            return null;
        }

        /// <summary>
        /// Resolves the <see cref="ServicePartitionKey"/> to send the message to, based on message type name.
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="brokerServiceName"></param>
        /// <returns></returns>
        private async Task<ServicePartitionKey> GetPartitionForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            var partitionKeys = await GetBrokerPartitionKeys(brokerServiceName);

            int hashCode;
            unchecked
            {
                hashCode = (int) _hashingHelper.HashString(messageTypeName);
            }
            int index = Math.Abs(hashCode % partitionKeys.Count);
            return partitionKeys[index];
        }

        /// <summary>
        /// Resolves the <see cref="ServicePartitionKey"/> to send the message to, based on message's type.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="brokerServiceName"></param>
        /// <returns></returns>
        private Task<ServicePartitionKey> GetPartitionForMessageAsync(object message, Uri brokerServiceName)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            string messageTypeName = message.GetType().FullName;
            return GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
        }

        private async Task<List<ServicePartitionKey>> GetBrokerPartitionKeys(Uri brokerServiceName = null)
        {
            if (_cachedPartitionKeys.Count == 0)
            {
                foreach (var partition in await _fabricClient.QueryManager.GetPartitionListAsync(brokerServiceName ?? await LocateAsync()))
                {
                    if (partition.PartitionInformation.Kind != ServicePartitionKind.Int64Range)
                    {
                        throw new InvalidOperationException("Sorry, only Int64 Range Partitions are supported.");
                    }

                    var info = (Int64RangePartitionInformation)partition.PartitionInformation;
                    _cachedPartitionKeys.Add(new ServicePartitionKey(info.LowKey));
                }
            }

            return _cachedPartitionKeys;
        }
    }
}
