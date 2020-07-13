using System;
using System.Fabric;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSub.State;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.Common
{
    internal class CustomProxyFactories : IProxyFactories
    {
        private readonly Lazy<IActorProxyFactory> _actorProxyFactory = new Lazy<IActorProxyFactory>(() => new ActorProxyFactory(
            (serviceRemotingCallbackMessageHandler) => new CustomActorRemotingClientFactory(serviceRemotingCallbackMessageHandler)));

        private readonly Lazy<IServiceProxyFactory> _serviceProxyFactory = new Lazy<IServiceProxyFactory>(() => new ServiceProxyFactory(
            (serviceRemotingCallbackMessageHandler) => new CustomServiceRemotingClientFactory(serviceRemotingCallbackMessageHandler)));

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

            return CreateServiceProxy<T>(serviceReference.ServiceUri, partitionKey, listenerName: serviceReference.ListenerName);
        }

        public T CreateServiceProxy<T>(Uri serviceUri, ServicePartitionKey partitionKey, string listenerName) where T : IService =>
             _serviceProxyFactory.Value.CreateServiceProxy<T>(serviceUri, partitionKey, listenerName: listenerName);

        public T CreateActorProxy<T>(ActorReference actorReference) where T : IActor =>
            CreateActorProxy<T>(actorReference.ServiceUri, actorReference.ActorId, actorReference.ListenerName);

        public T CreateActorProxy<T>(Uri serviceUri, ActorId actorId, string listenerName) where T : IActor =>
            _actorProxyFactory.Value.CreateActorProxy<T>(serviceUri, actorId, listenerName);
    }
}