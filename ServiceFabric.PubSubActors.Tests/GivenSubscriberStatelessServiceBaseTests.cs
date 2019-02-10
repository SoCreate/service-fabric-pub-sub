using System;
using System.Collections.Generic;
using System.Fabric;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Runtime;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(CancellationToken.None);

            Assert.AreEqual(1, service.Subscriptions.Count());
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessage {SomeValue = "SomeValue"}.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectOverloadMethodIsInvoked()
        {
            var service = new MockSubscriberStatelessServiceBase(MockStatelessServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessageSpecialized { SomeValue = "SomeValue" }.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

       

        public class MockSubscriberServiceHelper : ISubscriberServiceHelper
        {
            /// <inheritdoc />
            public Task RegisterMessageTypeAsync(StatelessService service, Type messageType, Uri brokerServiceName = null,
                string listenerName = null)
            {
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task UnregisterMessageTypeAsync(StatelessService service, Type messageType, bool flushQueue,
                Uri brokerServiceName = null)
            {
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task RegisterMessageTypeAsync(StatefulService service, Type messageType, Uri brokerServiceName = null,
                string listenerName = null)
            {
                return Task.CompletedTask;
            }

            /// <inheritdoc />
            public Task UnregisterMessageTypeAsync(StatefulService service, Type messageType, bool flushQueue,
                Uri brokerServiceName = null)
            {
                return Task.CompletedTask;
            }
        }


        public class MockSubscriberStatelessServiceBase : SubscriberStatelessServiceBase
        {
            public bool MethodCalled { get; private set; }

            internal new IEnumerable<Helpers.SubscriptionDefinition> Subscriptions => base.Subscriptions.Values;

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