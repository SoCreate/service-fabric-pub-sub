using System;
using System.Collections.Generic;
using System.Fabric;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ServiceFabric.Mocks;
using ServiceFabric.PubSubActors.Helpers;
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

namespace ServiceFabric.PubSubActors.Tests
{
    [TestClass]
    public class GivenSubscriberStatelessServiceBaseTests
    {
        [TestMethod]
        public async Task WhenMarkedServiceScansAttributes_ThenCorrectlyRegistered()
        {
            var helper = GetMockSubscriberServiceHelper();
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, helper.Object);
            await service.InvokeOnOpenAsync(CancellationToken.None);

            var subscriptions = typeof(SubscriberServiceHelper)
                .GetProperty("Subscriptions", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(helper.Object) as Dictionary<Type, SubscriptionDefinition>;
            Assert.AreEqual(1, subscriptions?.Count);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, GetMockSubscriberServiceHelper().Object);
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessage {SomeValue = "SomeValue"}.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectOverloadMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, GetMockSubscriberServiceHelper().Object);
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessageSpecialized { SomeValue = "SomeValue" }.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        private Mock<SubscriberServiceHelper> GetMockSubscriberServiceHelper()
        {
            var helper = new Mock<SubscriberServiceHelper>();
            helper.Setup(m => m.CreateServiceReference(It.IsAny<StatelessService>(), null))
                .Returns(new ServiceReference());

            return helper;
        }

        public class MockSubscriberStatelessServiceBase : SubscriberStatelessServiceBase
        {
            public bool MethodCalled { get; private set; }

            /// <inheritdoc />
            public MockSubscriberStatelessServiceBase(StatelessServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null) : base(serviceContext, subscriberServiceHelper)
            {
            }

            [Subscribe(typeof(MockMessage))]
            private Task HandleMockMessage(MockMessage message)
            {
                MethodCalled = true;
                return Task.CompletedTask;
            }
        }
    }
}