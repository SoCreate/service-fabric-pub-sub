using System;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Client;
using Microsoft.ServiceFabric.Services.Remoting;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub
{
    /// <summary>
    /// Specifies an interface for the factories that creates custom proxies for services and actors
    /// </summary>
    public interface IProxyFactories
    {
        /// <summary>
        /// Creates a service proxy using the provided service reference
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceReference"></param>
        /// <returns>
        ///     The proxy that implements the interface that is being remoted, should implement <see cref="IService"/>.
        /// </returns>
        T CreateServiceProxy<T>(ServiceReference serviceReference) where T : IService;

        /// <summary>
        /// Creates a service proxy using the provided service uri, partition key, and listener name (optional)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceReference"></param>
        /// <returns>
        ///     The proxy that implements the interface that is being remoted, should implement <see cref="IService"/>.
        /// </returns>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceUri"></param>
        /// <param name="partitionKey"></param>
        /// <param name="listenerName"></param>
        /// <returns>
        ///     The proxy that implements the interface that is being remoted, should implement <see cref="IService"/>.
        /// </returns>
        T CreateServiceProxy<T>(Uri serviceUri, ServicePartitionKey partitionKey, string listenerName = null) where T : IService;

        /// <summary>
        /// Creates a actor proxy using the provided actor reference
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="actorReference"></param>
        /// <returns>
        ///     The proxy that implements the interface that is being remoted, should implement <see cref="IActor"/>.
        /// </returns>
        T CreateActorProxy<T>(ActorReference actorReference) where T : IActor;

        /// <summary>
        /// Creates a actor proxy using the provided service uri, actor id, and listener name (optional)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="serviceUri"></param>
        /// <param name="actorId"></param>
        /// <param name="listenerName"></param>
        /// <returns>
        ///     The proxy that implements the interface that is being remoted, should implement <see cref="IActor"/>.
        /// </returns>
        T CreateActorProxy<T>(Uri serviceUri, ActorId actorId, string listenerName = null) where T : IActor;
    }
}