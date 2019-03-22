using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.Mocks;
using SoCreate.ServiceFabric.PubSub.Helpers;
using SoCreate.ServiceFabric.PubSub.State;
using SoCreate.ServiceFabric.PubSub.Subscriber;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenSubscriberStatelessServiceBaseTests
    {
        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, new BrokerClient(new MockBrokerServiceLocator()));
            service.SetPartition(new MockStatelessServicePartition
            {
                PartitionInfo = new SingletonPartitionInformation()
            });
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessage {SomeValue = "SomeValue"}.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectOverloadMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, new BrokerClient(new MockBrokerServiceLocator()));
            service.SetPartition(new MockStatelessServicePartition
            {
                PartitionInfo = new SingletonPartitionInformation()
            });
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessageSpecialized { SomeValue = "SomeValue" }.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        public class MockSubscriberStatelessServiceBase : SubscriberStatelessServiceBase
        {
            public bool MethodCalled { get; private set; }

            /// <inheritdoc />
            public MockSubscriberStatelessServiceBase(StatelessServiceContext serviceContext, IBrokerClient brokerClient = null) : base(serviceContext, brokerClient)
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