using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors.Remoting.V2.FabricTransport.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Communication.Client;
using Microsoft.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.V2;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;

namespace SoCreate.ServiceFabric.PubSubDemo.Common.Remoting
{
    internal class CustomActorRemotingClientFactory : IServiceRemotingClientFactory
    {
        private readonly IServiceRemotingClientFactory _innerServiceRemotingClientFactory;

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientConnected;

        public event EventHandler<CommunicationClientEventArgs<IServiceRemotingClient>> ClientDisconnected;

        public CustomActorRemotingClientFactory(IServiceRemotingCallbackMessageHandler serviceRemotingCallbackMessageHandler)
        {
            _innerServiceRemotingClientFactory = new FabricTransportActorRemotingClientFactory(
                            new FabricTransportRemotingSettings(), callbackMessageHandler: serviceRemotingCallbackMessageHandler, serializationProvider: new ServiceRemotingJsonSerializationProvider());
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