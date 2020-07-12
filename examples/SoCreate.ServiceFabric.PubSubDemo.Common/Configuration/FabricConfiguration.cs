using System.Fabric;
using System.Fabric.Description;
using SoCreate.ServiceFabric.PubSub;
using SoCreate.ServiceFabric.PubSub.Helpers;

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
            UseCustomServiceRemotingClientFactory ? new BrokerClient(new BrokerServiceLocator(proxyFactories: new CustomProxyFactories())) : null;

        public static IBrokerServiceLocator GetBrokerServiceLocator() =>
            UseCustomServiceRemotingClientFactory ? new BrokerServiceLocator(proxyFactories: new CustomProxyFactories()) : null;

        public static IProxyFactories GetProxyFactories() =>
            UseCustomServiceRemotingClientFactory ? new CustomProxyFactories() : null;

        private static string ReadConfiguration(string key) => activationContextConfigurationSection.Parameters[key].Value;
    }
}