using System;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Services.Client;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Tests
{
    public class MockBrokerServiceLocator : IBrokerServiceLocator
    {
        public Task<Uri> LocateAsync()
        {
            return Task.FromResult(new Uri("mockUri"));
        }

        public Task RegisterAsync(Uri brokerServiceName)
        {
            return Task.CompletedTask;
        }

        public Task<ServicePartitionKey> GetPartitionForMessageAsync(string messageTypeName, Uri brokerServiceName)
        {
            return Task.FromResult<ServicePartitionKey>(null);
        }

        public Task<ServicePartitionKey> GetPartitionForMessageAsync(object message, Uri brokerServiceName)
        {
            return Task.FromResult<ServicePartitionKey>(null);
        }

        public Task<IBrokerService> GetBrokerServiceForMessageAsync(object message, Uri brokerServiceName = null)
        {
            return Task.FromResult<IBrokerService>(new MockBrokerService());
        }

        public Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName = null)
        {
            return Task.FromResult<IBrokerService>(new MockBrokerService());
        }
    }

    public class MockBrokerService : IBrokerService
    {
        public Task RegisterSubscriberAsync(ActorReference actor, string messageTypeName, string routingKey = null)
        {
            return Task.CompletedTask;
        }

        public Task UnregisterSubscriberAsync(ActorReference actor, string messageTypeName, bool flushQueue)
        {
            return Task.CompletedTask;
        }

        public Task RegisterServiceSubscriberAsync(ServiceReference service, string messageTypeName, string routingKey = null)
        {
            return Task.CompletedTask;
        }

        public Task UnregisterServiceSubscriberAsync(ServiceReference service, string messageTypeName, bool flushQueue)
        {
            return Task.CompletedTask;
        }

        public Task SubscribeAsync(ReferenceWrapper reference, string messageTypeName)
        {
            return Task.CompletedTask;
        }

        public Task UnsubscribeAsync(ReferenceWrapper reference, string messageTypeName, bool flushQueue)
        {
            return Task.CompletedTask;
        }

        public Task PublishMessageAsync(MessageWrapper message)
        {
            return Task.CompletedTask;
        }
    }
}