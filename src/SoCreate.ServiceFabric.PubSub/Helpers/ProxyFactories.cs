using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using Microsoft.ServiceFabric.Services.Remoting.V2.FabricTransport.Client;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Helpers
{
    public class ProxyFactories : IProxyFactories
    {
        private readonly Lazy<IServiceProxyFactory> _serviceProxyFactory = new Lazy<IServiceProxyFactory>(() => new ServiceProxyFactory(c => new FabricTransportServiceRemotingClientFactory()));

        public T CreateActorProxy<T>(ActorReference actorReference) where T : IActor =>
            (T)actorReference.Bind(typeof(T));

        public T CreateActorProxy<T>(Uri serviceUri, ActorId actorId, string listenerName) where T : IActor =>
            CreateActorProxy<T>(new ActorReference { ServiceUri = serviceUri, ActorId = actorId, ListenerName = listenerName });

        public T CreateServiceProxy<T>(Uri serviceUri, ServicePartitionKey partitionKey, string listenerName) where T : IService =>
            _serviceProxyFactory.Value.CreateServiceProxy<T>(serviceUri, partitionKey, listenerName: listenerName);

        public T CreateServiceProxy<T>(ServiceReference serviceReference) where T : IService
        {
            ServicePartitionKey partitionKey;
            switch (serviceReference.PartitionKind)
            {
                case ServicePartitionKind.Singleton:
                    partitionKey = ServicePartitionKey.Singleton;
                    break;

                case ServicePartitionKind.Int64Range:
                    partitionKey = new ServicePartitionKey(serviceReference.PartitionKey);
                    break;

                case ServicePartitionKind.Named:
                    partitionKey = new ServicePartitionKey(serviceReference.PartitionName);
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            return CreateServiceProxy<T>(serviceReference.ServiceUri, partitionKey, serviceReference.ListenerName);
        }
    }
}