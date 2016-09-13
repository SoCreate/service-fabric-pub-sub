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
        public async Task RegisterAsync(Uri brokerServiceName)
        {
            var activationContext = FabricRuntime.GetActivationContext();
            var fc = new FabricClient();
            await fc.PropertyManager.PutPropertyAsync(new Uri(activationContext.ApplicationName), nameof(BrokerService), brokerServiceName.ToString());
        }

        public async Task<Uri> LocateAsync()
        {
            try
            {
                var activationContext = FabricRuntime.GetActivationContext();
                var fc = new FabricClient();
                var property = await fc.PropertyManager.GetPropertyAsync(new Uri(activationContext.ApplicationName), nameof(BrokerService));
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

        private static ServicePartitionList _cachedPartitions;

        /// <summary>
        /// Resolves the <see cref="ServicePartitionKey"/> to send the message to, based on message type.
        /// </summary>
        /// <param name="message">The message to publish</param>
        /// <param name="brokerServiceName"></param>
        /// <returns></returns>
        public async Task<ServicePartitionKey> GetPartitionForMessageAsync(object message, Uri brokerServiceName)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (brokerServiceName == null) throw new ArgumentNullException(nameof(brokerServiceName));

            string messageTypeName = (message.GetType().FullName);

            if (_cachedPartitions == null)
            {
                var fabricClient = new FabricClient();
                _cachedPartitions = await fabricClient.QueryManager.GetPartitionListAsync(brokerServiceName);
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

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="message"/>
        /// </summary>
        /// <param name="message"></param>
        /// <param name="brokerServiceName">Uri of BrokerService instance</param>
        /// <returns></returns>
        public Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName)
        {
            return GetBrokerServiceForMessageAsync(message.GetType().FullName, brokerServiceName);
        }

        /// <summary>
        /// Gets the <see cref="IBrokerService"/> instance for the provided <paramref name="messageTypeName"/>
        /// </summary>
        /// <param name="messageTypeName">Full type name of message object.</param>
        /// <param name="brokerServiceName">Uri of BrokerService instance</param>
        /// <returns></returns>
        public async Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            var resolvedPartition = await GetPartitionForMessageAsync(messageTypeName, brokerServiceName);
            var brokerService = ServiceProxy.Create<IBrokerService>(brokerServiceName, resolvedPartition, listenerName: BrokerService.ListenerName);
            return brokerService;
        }
    }
}
