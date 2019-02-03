using System;
using System.Fabric;
using System.Reflection;
using Microsoft.ServiceFabric.Services.Runtime;

namespace ServiceFabric.PubSubActors.Helpers
{
    public class SubscriberHelperExtensions
    {
        public static ISubscriberHelper GetFor(StatelessService service, IBrokerServiceLocator locator = null)
        {
            return new SubscriberHelper(service.Context, new Lazy<ServicePartitionInformation>(() => GetServicePartition(service)?.PartitionInfo), locator);
        }

        public static ISubscriberHelper GetFor(StatefulService service, IBrokerServiceLocator locator = null)
        {
            return new SubscriberHelper(service.Context, new Lazy<ServicePartitionInformation>(()=> GetServicePartition(service)?.PartitionInfo), locator);
        }

        private static IStatelessServicePartition GetServicePartition(StatelessService serviceBase)
        {
            return GetServicePartition<IStatelessServicePartition>(serviceBase);
        }

        private static IStatefulServicePartition GetServicePartition(StatefulService serviceBase)
        {
            return GetServicePartition<IStatefulServicePartition>(serviceBase);
        }

        private static TPartition GetServicePartition<TPartition>(object serviceBase) where TPartition : IServicePartition
        {
            if (serviceBase == null) 
                throw new ArgumentNullException(nameof(serviceBase));
            var prop = serviceBase.GetType().GetProperty("Partition", BindingFlags.Instance | BindingFlags.NonPublic);

            return (TPartition) prop?.GetValue(serviceBase);
        }
    }
}