using System;
using System.Threading.Tasks;
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