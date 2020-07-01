using System;
using System.Fabric;
using System.Fabric.Description;
using Microsoft.ServiceFabric.Services.Remoting.V2.Client;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSubDemo.Common.Remoting;

namespace SoCreate.ServiceFabric.PubSubDemo.Common.Configuration
{
    public static class FabricConfiguration
    {
        private static readonly ConfigurationSection activationContextConfigurationSection = FabricRuntime.GetActivationContext()
                .GetConfigurationPackageObject("Config")
                .Settings
                .Sections["ConfigurationSettings"];

        public static bool UseCustomServiceRemotingClientFactory => bool.Parse(ReadConfiguration("UseCustomServiceRemotingClientFactory"));

        public static IBrokerClient GetBrokerClient() =>
            UseCustomServiceRemotingClientFactory ? new BrokerClient(new JsonSerializationBrokerServiceLocator()) : null;

        public static IProxyFactories GetProxyFactories() =>
            UseCustomServiceRemotingClientFactory ? new CustomProxyFactories() : null;

        public static Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory> ActorRemotingClientFactoryFunc =>
            UseCustomServiceRemotingClientFactory
                    ? (serviceRemotingCallbackMessageHandler) => new CustomActorRemotingClientFactory(serviceRemotingCallbackMessageHandler)
                    : (Func<IServiceRemotingCallbackMessageHandler, IServiceRemotingClientFactory>)null;

        private static string ReadConfiguration(string key)
        {
            return activationContextConfigurationSection.Parameters[key].Value;
        }
    }
}