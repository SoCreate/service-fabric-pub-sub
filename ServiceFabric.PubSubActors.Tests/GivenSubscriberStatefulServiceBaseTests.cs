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
    public class GivenSubscriberStatefulServiceBaseTests
    {
        [TestMethod]
        public async Task WhenMarkedServiceScansAttributes_ThenCorrectlyRegistered()
        {
            var service = new MockSubscriberStatefulServiceBase(MockStatefulServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(ReplicaOpenMode.New, CancellationToken.None);

            Assert.AreEqual(1, service.Handlers.Count());
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectMethodIsInvoked()
        {
            var service = new MockSubscriberStatefulServiceBase(MockStatefulServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(ReplicaOpenMode.New, CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessage {SomeValue = "SomeValue"}.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        [TestMethod]
        public async Task WhenMarkedServiceReceivesMessage_ThenCorrectOverloadMethodIsInvoked()
        {
            var service = new MockSubscriberStatefulServiceBase(MockStatefulServiceContextFactory.Default, new MockSubscriberServiceHelper());
            await service.InvokeOnOpenAsync(ReplicaOpenMode.New, CancellationToken.None);
            await service.ReceiveMessageAsync(new MockMessageSpecialized { SomeValue = "SomeValue" }.CreateMessageWrapper());
            Assert.IsTrue(service.MethodCalled);
        }

        public class MockSubscriberServiceHelper : ISubscriberServiceHelper
        {
            private ISubscriberServiceHelper _helper;

            public MockSubscriberServiceHelper()
            {
                _helper = new SubscriberServiceHelper();
            }
            public Task RegisterMessageTypeAsync(StatelessService service, Type messageType, Uri brokerServiceName = null,
                string listenerName = null)
            {
                return Task.CompletedTask;
            }

            public Task UnregisterMessageTypeAsync(StatelessService service, Type messageType, bool flushQueue,
                Uri brokerServiceName = null)
            {
                return Task.CompletedTask;
            }

            public Task RegisterMessageTypeAsync(StatefulService service, Type messageType, Uri brokerServiceName = null,
                string listenerName = null)
            {
                return Task.CompletedTask;
            }

            public Task UnregisterMessageTypeAsync(StatefulService service, Type messageType, bool flushQueue,
                Uri brokerServiceName = null)
            {
                return Task.CompletedTask;
            }

            public Dictionary<Type, Func<object, Task>> DiscoverMessageHandlers<T>(T handlerClass) where T : class
            {
                return _helper.DiscoverMessageHandlers(handlerClass);
            }

            public Task SubscribeAsync(ServiceReference serviceReference, IEnumerable<Type> messageTypes, Uri broker = null)
            {
                return _helper.SubscribeAsync(serviceReference, messageTypes, broker);
            }

            public Task ProccessMessageAsync(MessageWrapper messageWrapper, Dictionary<Type, Func<object, Task>> handlers)
            {
                return _helper.ProccessMessageAsync(messageWrapper, handlers);
            }

            public ServiceReference CreateServiceReference(StatelessService service, string listenerName = null)
            {
                return new ServiceReference();
            }

            public ServiceReference CreateServiceReference(StatefulService service, string listenerName = null)
            {
                return new ServiceReference();
            }
        }

        public class MockSubscriberStatefulServiceBase : SubscriberStatefulServiceBase
        {
            public bool MethodCalled { get; private set; }

            internal new IEnumerable<Func<object, Task>> Handlers => base.Handlers.Values;

            /// <inheritdoc />
            public MockSubscriberStatefulServiceBase(StatefulServiceContext serviceContext, ISubscriberServiceHelper subscriberServiceHelper = null) : base(serviceContext, subscriberServiceHelper)
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