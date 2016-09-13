using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Client;

namespace ServiceFabric.PubSubActors.Helpers
{
    public interface IBrokerServiceLocator
    {
        Task<Uri> LocateAsync();
        Task RegisterAsync(Uri brokerServiceName);
        Task<ServicePartitionKey> GetPartitionForMessageAsync(object message, Uri brokerServiceName);
        Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName);
        Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName);
    }
}