using System;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;

namespace SoCreate.ServiceFabric.PubSub
{
    /// <summary>
    /// Provides a custom proxy remoting factories for actors and services
    /// </summary>
    public interface IProxyFactories
    {
        /// <summary>
        /// Gets the remoting client factory for services
        /// </summary>
        /// <returns>
        ///     A functor delegate that creates an implementation of IServiceRemotingClientFactory for a Service
        ///     <see cref="IServiceRemotingClientFactory"/>
        /// </returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public <IServiceRemotingClientFactory> GetServiceRemotingClientFactory() =>
        ///     (serviceRemotingCallbackMessageHandler) => new CustomServiceRemotingClientFactory(serviceRemotingCallbackMessageHandler)
        /// ]]>
        /// </code>
        /// </example>
        Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> GetServiceRemotingClientFactory();

        /// <summary>
        /// Gets the remoting client factory for actors
        /// </summary>
        /// <returns>
        ///     A functor delegate that creates an implementation of IServiceRemotingClientFactory for an Actor
        ///     <see cref="IServiceRemotingClientFactory"/>
        /// </returns>
        /// <example>
        /// <code>
        /// <![CDATA[
        /// public Func <IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> GetActorRemotingClientFactory() =>
        ///     (actorRemotingCallbackMessageHandler) => new CustomActorRemotingClientFactory(actorRemotingCallbackMessageHandler)
        /// ]]>
        /// </code>
        /// </example>
        Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> GetActorRemotingClientFactory();
    }
}