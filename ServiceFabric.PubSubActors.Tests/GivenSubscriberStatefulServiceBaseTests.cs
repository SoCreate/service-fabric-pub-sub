using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.State;
using ServiceFabric.PubSubActors.Subscriber;

namespace ServiceFabric.PubSubActors.Tests
{
    [TestClass]
    public class GivenSubscriberStatefulServiceBaseTests
    {
        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectMethodIsInvoked()
        {
            var service = new MockSubscriberStatefulServiceBase(MockStatefulServiceContextFactory.Default, new BrokerClient(new MockBrokerServiceLocator()));
            service.SetPartition(new MockStatefulServicePartition
            {
                PartitionInfo = new SingletonPartitionInformation()
            });
            await service.InvokeOnOpenAsync(ReplicaOpenMode.New, CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessage {SomeValue = "SomeValue"}.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectOverloadMethodIsInvoked()
        {
            var service = new MockSubscriberStatefulServiceBase(MockStatefulServiceContextFactory.Default, new BrokerClient(new MockBrokerServiceLocator()));
            service.SetPartition(new MockStatefulServicePartition
            {
                PartitionInfo = new SingletonPartitionInformation()
            });
            await service.InvokeOnOpenAsync(ReplicaOpenMode.New, CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessageSpecialized { SomeValue = "SomeValue" }.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        public class MockSubscriberStatefulServiceBase : SubscriberStatefulServiceBase
        {
            public bool MethodCalled { get; private set; }

            /// <inheritdoc />
            public MockSubscriberStatefulServiceBase(StatefulServiceContext serviceContext, IBrokerClient brokerClient = null) : base(serviceContext, brokerClient)
            {
            }

            [Subscribe]
            private Task HandleMockMessage(MockMessage message)
            {
                MethodCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}