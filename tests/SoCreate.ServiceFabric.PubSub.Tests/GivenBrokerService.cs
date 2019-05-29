using System;
using System.Fabric;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;
using SoCreate.ServiceFabric.PubSub.Events;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenBrokerService
    {
        [TestMethod]
        public async Task WhenMessageTypeIsSubscribedTo_ThenSubscribedEventsAreFired()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.SubscribeAsync(GetMockReferenceWrapper(), "MyMessageType");
            Assert.AreEqual(1, broker.SubscribeEventCount);
        }

        [TestMethod]
        public async Task WhenMessageTypeIsUnsubscribedTo_ThenUnsubscribedEventsAreFired()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.UnsubscribeAsync(GetMockReferenceWrapper(), "MyMessageType");
            Assert.AreEqual(1, broker.UnsubscribeEventCount);
        }

        [TestMethod]
        public async Task WhenMessageIsPublished_ThenMessagePublishedEventsAreFired()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.PublishMessageAsync(new TestMessage().CreateMessageWrapper());

            // Note: This message has no subscribers
            Assert.AreEqual(1, broker.MessagePublishedEventCount);
            Assert.AreEqual(0, broker.MessageQueuedToSubscriberEventCount);
        }

        [TestMethod]
        public async Task WhenMessageIsPublished_ThenMessageQueuedToSubscriberEventsAreFiredForEachSubscriber()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber1"), typeof(TestMessage).FullName);
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber2"), typeof(TestMessage).FullName);

            await broker.PublishMessageAsync(new TestMessage().CreateMessageWrapper());
            Assert.AreEqual(1, broker.MessagePublishedEventCount);
            Assert.AreEqual(2, broker.MessageQueuedToSubscriberEventCount);
        }

        [TestMethod]
        public async Task WhenMessageIsDelivered_ThenMessageDeliveredEventsAreFired()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber1"), typeof(TestMessage).FullName);
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber2"), typeof(TestMessage).FullName);

            await broker.PublishMessageAsync(new TestMessage().CreateMessageWrapper());

            await ExecuteRunAsync(broker);
            Assert.AreEqual(2, broker.MessageDeliveredEventCount);
            Assert.AreEqual(0, broker.MessageDeliveryFailedEventCount);
        }

        [TestMethod]
        public async Task WhenMessageDeliveryFails_ThenMessageDeliveryFailedEventsAreFired()
        {
            var broker = new MockBrokerServiceWithEvents(MockStatefulServiceContextFactory.Default, new MockReliableStateManager());
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber1"), typeof(TestMessage).FullName);
            await broker.SubscribeAsync(GetMockReferenceWrapper("Subscriber2", true), typeof(TestMessage).FullName);

            await broker.PublishMessageAsync(new TestMessage().CreateMessageWrapper());

            await ExecuteRunAsync(broker);
            Assert.AreEqual(1, broker.MessageDeliveredEventCount);
            Assert.AreEqual(1, broker.MessageDeliveryFailedEventCount);
        }

        private async Task ExecuteRunAsync(BrokerService broker)
        {
            // Execute RunAsync to process queues.
            var processQueuesMethod = typeof(MockBrokerServiceWithEvents).GetMethod("RunAsync", BindingFlags.NonPublic | BindingFlags.Instance);
            var cancellationToken = new CancellationToken();
            var cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            processQueuesMethod.Invoke(broker, new object[] { cancellationToken });
            // Wait long enough for the queues to be processed, then kill it so it doesn't loop forever.
            await Task.Delay(TimeSpan.FromMilliseconds(50));
            cancellationSource.Cancel();
        }

        private ReferenceWrapper GetMockReferenceWrapper(string name = "Subscriber", bool isBroken = false)
        {
            var reference = new ServiceReference
            {
                ServiceUri = new Uri($"fabric://MockService/{name}"),
                PartitionKind = ServicePartitionKind.Singleton
            };
            return new MockServiceReferenceWrapper(reference, isBroken);
        }
    }

    public class TestMessage {}

    public class MockServiceReferenceWrapper : ServiceReferenceWrapper
    {
        private readonly bool _isBroken;

        public MockServiceReferenceWrapper(ServiceReference serviceReference, bool isBroken = false) : base(serviceReference)
        {
            _isBroken = isBroken;
        }

        public override Task PublishAsync(MessageWrapper message)
        {
            if (_isBroken)
            {
                throw new Exception("The subscriber threw and exception");
            }
            return Task.CompletedTask;
        }
    }

    public class MockBrokerServiceWithEvents : BrokerService
    {
        public int SubscribeEventCount { get; private set; }
        public int UnsubscribeEventCount { get; private set; }
        public int MessagePublishedEventCount { get; private set; }
        public int MessageQueuedToSubscriberEventCount { get; private set; }
        public int MessageDeliveredEventCount { get; private set; }
        public int MessageDeliveryFailedEventCount { get; private set; }

        public MockBrokerServiceWithEvents(StatefulServiceContext serviceContext, bool enableAutoDiscovery = false, IBrokerEventsManager brokerEventsManager = null) : base(serviceContext, enableAutoDiscovery, brokerEventsManager)
        {
        }

        public MockBrokerServiceWithEvents(StatefulServiceContext serviceContext, IReliableStateManagerReplica2 reliableStateManagerReplica, bool enableAutoDiscovery = false, IBrokerEventsManager brokerEventsManager = null) : base(serviceContext, reliableStateManagerReplica, enableAutoDiscovery, brokerEventsManager)
        {
            DueTime = TimeSpan.Zero;
            Period = TimeSpan.FromMilliseconds(50);
        }

        protected override void SetupEvents(IBrokerEvents events)
        {
            events.Subscribed += (queueName, subscriber, messageType) =>
            {
                SubscribeEventCount++;
                return Task.CompletedTask;
            };

            events.Unsubscribed += (queueName, subscriber, messageType) =>
            {
                UnsubscribeEventCount++;
                return Task.CompletedTask;
            };

            events.MessagePublished += messageType =>
            {
                MessagePublishedEventCount++;
                return Task.CompletedTask;
            };

            events.MessageQueuedToSubscriber += (queueName, subscriber, messageType) =>
            {
                MessageQueuedToSubscriberEventCount++;
                return Task.CompletedTask;
            };

            events.MessageDelivered += (queueName, subscriber, messageType) =>
            {
                MessageDeliveredEventCount++;
                return Task.CompletedTask;
            };

            events.MessageDeliveryFailed += (queueName, subscriber, messageType, exception) =>
            {
                MessageDeliveryFailedEventCount++;
                return Task.CompletedTask;
            };
        }
    }
}
