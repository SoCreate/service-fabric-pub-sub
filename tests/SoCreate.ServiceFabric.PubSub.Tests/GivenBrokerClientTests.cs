using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenBrokerClientTests
    {
        [TestMethod]
        public async Task WhenGetBrokerStatsIsEmpty_EmptyDictionaryIsReturned()
        {
            var brokerClient = new BrokerClient(new MockBrokerServiceLocatorMultiPartition(new List<IBrokerService>
            {
                new MockBrokerService(),
                new MockBrokerService()
            }));

            var queueStats = await brokerClient.GetBrokerStatsAsync();
            Assert.IsTrue(queueStats.Count == 0);
        }

        [TestMethod]
        public async Task WhenGetBrokerStatsIsCalled_AggregatesBrokerStatsFromMultiplePartitions()
        {
            var brokerClient = new BrokerClient(new MockBrokerServiceLocatorMultiPartition());
            var queueStats = await brokerClient.GetBrokerStatsAsync();
            Assert.IsTrue(queueStats.Count == 2);
            Assert.IsTrue(queueStats.ContainsKey("MessageOneQueue") && queueStats["MessageOneQueue"].Count == 1 && queueStats["MessageOneQueue"].First().TotalDelivered == 34);
            Assert.IsTrue(queueStats.ContainsKey("MessageTwoQueue") && queueStats["MessageTwoQueue"].Count == 1 && queueStats["MessageTwoQueue"].First().TotalReceived == 500);
        }

        [TestMethod]
        public async Task WhenGetBrokerStatsIsCalledAgain_StatsAreAddedToTheList()
        {
            var brokerClient = new BrokerClient(new MockBrokerServiceLocatorMultiPartition());
            await brokerClient.GetBrokerStatsAsync();
            var queueStats = await brokerClient.GetBrokerStatsAsync();
            Assert.IsTrue(queueStats.Count == 2);
            Assert.IsTrue(queueStats.ContainsKey("MessageOneQueue") && queueStats["MessageOneQueue"].Count == 2 && queueStats["MessageOneQueue"].Last().TotalDelivered == 39);
            Assert.IsTrue(queueStats.ContainsKey("MessageTwoQueue") && queueStats["MessageTwoQueue"].Count == 2 && queueStats["MessageTwoQueue"].Last().TotalReceived == 510);
        }

        [TestMethod]
        public async Task WhenGetQueueCapacityIsExceeded_OldestStatsAreTrimmed()
        {
            var brokerClient = new BrokerClient(new MockBrokerServiceLocatorMultiPartition())
            {
                QueueStatCapacity = 2
            };
            await brokerClient.GetBrokerStatsAsync();
            await brokerClient.GetBrokerStatsAsync();
            var queueStats = await brokerClient.GetBrokerStatsAsync();
            Assert.IsTrue(queueStats.Count == 2);
            Assert.IsTrue(queueStats.ContainsKey("MessageOneQueue") && queueStats["MessageOneQueue"].Count == 2 && queueStats["MessageOneQueue"].First().TotalDelivered == 39);
            Assert.IsTrue(queueStats.ContainsKey("MessageTwoQueue") && queueStats["MessageTwoQueue"].Count == 2 && queueStats["MessageTwoQueue"].First().TotalReceived == 510);
        }

        [TestMethod]
        public async Task WhenUnsubscribeByQueueName_UnsubscribeIsCalledWithTheRightReferenceWrapper()
        {
            var brokerService = new MockBrokerServicePartitionOne();
            var brokerClient = new BrokerClient(new MockBrokerServiceLocatorMultiPartition(new List<IBrokerService> { brokerService }));
            await brokerClient.UnsubscribeByQueueNameAsync("MessageOneQueue");
            Assert.IsTrue(brokerService.UnsubscribeMessageTypeName == "MessageOneQueue");
            Assert.IsInstanceOfType(brokerService.UnsubscribeReference, typeof(ServiceReferenceWrapper));
        }
    }

    internal class MockBrokerServiceLocatorMultiPartition : MockBrokerServiceLocator
    {
        private readonly List<IBrokerService> _brokers;

        public MockBrokerServiceLocatorMultiPartition(List<IBrokerService> brokers = null)
        {
            _brokers = brokers ?? new List<IBrokerService>
            {
                new MockBrokerServicePartitionOne(),
                new MockBrokerServicePartitionTwo()
            };
        }
        public override Task<IEnumerable<IBrokerService>> GetBrokerServicesForAllPartitionsAsync(Uri brokerServiceName = null)
        {
            return Task.FromResult<IEnumerable<IBrokerService>>(_brokers);
        }

        public override Task<IBrokerService> GetBrokerServiceForMessageAsync(string messageTypeName, Uri brokerServiceName = null)
        {
            return Task.FromResult(_brokers.First());
        }
    }

    internal class MockBrokerServicePartitionOne : MockBrokerService
    {
        private ulong _totalDelivered = 29;
        private ulong _totalReceived = 45;

        public override Task<QueueStatsWrapper> GetBrokerStatsAsync()
        {
            _totalDelivered += 5;
            _totalReceived += 5;
            return Task.FromResult(new QueueStatsWrapper
            {
                Queues = new Dictionary<string, ReferenceWrapper>
                {
                    { "MessageOneQueue", new ServiceReferenceWrapper(new ServiceReference()) }
                },
                Stats =  new List<QueueStats>
                {
                    new QueueStats
                    {
                        QueueName = "MessageOneQueue",
                        Time = new DateTime(2000, 1, 1, 12, 0, 0),
                        TotalDelivered = _totalDelivered,
                        TotalReceived = _totalReceived
                    }
                }
            });
        }

        public override Task UnsubscribeAsync(ReferenceWrapper reference, string messageTypeName)
        {
            UnsubscribeReference = reference;
            UnsubscribeMessageTypeName = messageTypeName;

            return Task.CompletedTask;
        }

        public string UnsubscribeMessageTypeName { get; set; }

        public ReferenceWrapper UnsubscribeReference { get; set; }
    }

    internal class MockBrokerServicePartitionTwo : MockBrokerService
    {
        private ulong _totalDelivered = 644;
        private ulong _totalReceived = 490;

        public override Task<QueueStatsWrapper> GetBrokerStatsAsync()
        {
            _totalDelivered += 10;
            _totalReceived += 10;
            return Task.FromResult(new QueueStatsWrapper
            {
                Queues = new Dictionary<string, ReferenceWrapper>
                {
                    { "MessageTwoQueue", new ServiceReferenceWrapper(new ServiceReference()) }
                },
                Stats =  new List<QueueStats>
                {
                    new QueueStats
                    {
                        QueueName = "MessageTwoQueue",
                        Time = new DateTime(2000, 1, 1, 12, 0, 0),
                        TotalDelivered = _totalDelivered,
                        TotalReceived = _totalReceived
                    }
                }
            });
        }
    }
}