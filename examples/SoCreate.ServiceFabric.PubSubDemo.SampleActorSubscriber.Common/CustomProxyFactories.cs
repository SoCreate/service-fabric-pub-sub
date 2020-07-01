using System;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.Common
{
    internal class CustomProxyFactories : IProxyFactories
    {
        public Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> GetActorRemotingClientFactory() =>
            (serviceRemotingCallbackMessageHandler) => new CustomActorRemotingClientFactory(serviceRemotingCallbackMessageHandler);

        public Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> GetServiceRemotingClientFactory() =>
            (serviceRemotingCallbackMessageHandler) => new CustomServiceRemotingClientFactory(serviceRemotingCallbackMessageHandler);
    }
}