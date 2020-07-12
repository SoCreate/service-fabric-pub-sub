using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;

namespace SoCreate.ServiceFabric.PubSubDemo.Common.Remoting
{
    internal class CustomServiceRemotingClientFactory : IServiceRemotingClientFactory
    {
        private static readonly FabricTransportRemotingSettings RemotingSettings = new FabricTransportRemotingSettings
        {
            MaxMessageSize = 50 * 1024 * 1024, // 50 MB
            OperationTimeout = TimeSpan.FromHours(5),
        };

        private readonly IServiceRemotingClientFactory _innerServiceRemotingClientFactory;

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected
        {
            add { _innerServiceRemotingClientFactory.ClientDisconnected += value; }
            remove { _innerServiceRemotingClientFactory.ClientDisconnected -= value; }
        }

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected
        {
            add { _innerServiceRemotingClientFactory.ClientDisconnected += value; }
            remove { _innerServiceRemotingClientFactory.ClientDisconnected -= value; }
        }

        public CustomServiceRemotingClientFactory(IServiceRemotingCallbackMessageHandler serviceRemotingCallbackMessageHandler)
        {
            _innerServiceRemotingClientFactory = new FabricTransportServiceRemotingClientFactory(
                            RemotingSettings, remotingCallbackMessageHandler: serviceRemotingCallbackMessageHandler, serializationProvider: new ServiceRemotingJsonSerializationProvider());
        }

        public Task<IServiceRemotingClient> GetClientAsync(Uri serviceUri, ServicePartitionKey partitionKey, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            return _innerServiceRemotingClientFactory.GetClientAsync(serviceUri, partitionKey, targetReplicaSelector, listenerName, retrySettings, cancellationToken);
        }

        public Task<IServiceRemotingClient> GetClientAsync(ResolvedServicePartition previousRsp, TargetReplicaSelector targetReplicaSelector, string listenerName, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            return _innerServiceRemotingClientFactory.GetClientAsync(previousRsp, targetReplicaSelector, listenerName, retrySettings, cancellationToken);
        }

        public IServiceRemotingMessageBodyFactory GetRemotingMessageBodyFactory()
        {
            return _innerServiceRemotingClientFactory.GetRemotingMessageBodyFactory();
        }

        public Task<OperationRetryControl> ReportOperationExceptionAsync(IServiceRemotingClient client, ExceptionInformation exceptionInformation, OperationRetrySettings retrySettings, CancellationToken cancellationToken)
        {
            return _innerServiceRemotingClientFactory.ReportOperationExceptionAsync(client, exceptionInformation, retrySettings, cancellationToken);
        }
    }
}