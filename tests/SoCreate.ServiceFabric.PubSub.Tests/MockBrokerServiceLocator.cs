using System.Collections.Generic;
using System.Threading.Tasks;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    public class MockBrokerServiceLocator : IBrokerServiceLocator
    {
        public Task RegisterAsync()
        {
            return Task.CompletedTask;
        }

        public Task<IBrokerService> GetBrokerServiceForMessageAsync(object message)
        {
            return Task.FromResult<IBrokerService>(new MockBrokerService());
        }

        public virtual Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName)
        {
            return Task.FromResult<IBrokerService>(new MockBrokerService());
        }

        public virtual Task<IEnumerable<IBrokerService>> GetBrokerServicesForAllPartitionsAsync()
        {
            return Task.FromResult<IEnumerable<IBrokerService>>(new List<IBrokerService>
            {
                new MockBrokerService()
            });
        }
    }

    public class MockBrokerService : IBrokerService
    {
        public Task SubscribeAsync(ReferenceWrapper reference, string messageTypeName, bool isOrdered)
        {
            return Task.CompletedTask;
        }

        public virtual Task UnsubscribeAsync(ReferenceWrapper reference, string messageTypeName)
        {
            return Task.CompletedTask;
        }

        public Task PublishMessageAsync(MessageWrapper message)
        {
            return Task.CompletedTask;
        }

        public virtual Task<QueueStatsWrapper> GetBrokerStatsAsync()
        {
            return Task.FromResult(new QueueStatsWrapper
            {
                Queues = new Dictionary<string, ReferenceWrapper>(),
                Stats = new List<QueueStats>()
            });
        }
    }
}