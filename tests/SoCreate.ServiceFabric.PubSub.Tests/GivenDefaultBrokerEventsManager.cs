using Microsoft.ServiceFabric.Actors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoCreate.ServiceFabric.PubSub.Events;
using SoCreate.ServiceFabric.PubSub.State;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenDefaultBrokerEventsManager
    {
        [TestMethod]
        public async Task WhenSubscribedEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.Subscribed += (queueName, subscriber, messageType) =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnSubscribedAsync("myqueue", new ServiceReferenceWrapper(new ServiceReference()), "myMessageType");

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task WhenUnsubscribedEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.Unsubscribed += (queueName, subscriber, messageType) =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnUnsubscribedAsync("myqueue", new ServiceReferenceWrapper(new ServiceReference()), "myMessageType");

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task WhenMessagePublishedEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.MessagePublished += message =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnMessagePublishedAsync(new MessageWrapper());

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task WhenMessageQueuedToSubscriberEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.MessageQueuedToSubscriber += (queueName, subscriber, messageType) =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnMessageQueuedToSubscriberAsync("myqueue", new ServiceReferenceWrapper(new ServiceReference()), new MessageWrapper());

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task WhenMessageDeliveredEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.MessageDelivered += (queueName, subscriber, messageType) =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnMessageDeliveredAsync("myqueue", new ServiceReferenceWrapper(new ServiceReference()), new MessageWrapper());

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public async Task WhenMessageDeliveryFailedEventsAreAdded_ThenCallbacksAreExecuted()
        {
            var count = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.MessageDeliveryFailed += (queueName, subscriber, messageType, exception) =>
            {
                count++;
                return Task.CompletedTask;
            };
            await manager.OnMessageDeliveryFailedAsync("myqueue", new ServiceReferenceWrapper(new ServiceReference()), new MessageWrapper(), new Exception("Failed to deliver"), 10);

            Assert.AreEqual(1, count);
        }

        [TestMethod]
        public void WhenInsertingConcurrently_ThenAllCallbacksAreMadeCorrectly()
        {
            bool hasCrashed = false;
            var manager = new DefaultBrokerEventsManager();
            ManualResetEvent mr = new ManualResetEvent(false);
            ManualResetEvent mr2 = new ManualResetEvent(false);

            manager.Subscribed += (s, e, t) =>
            {
                mr.WaitOne();
                return Task.CompletedTask;
            };

            const int attempts = 2000;

            for (int i = 0; i < attempts; i++)
            {
                ThreadPool.QueueUserWorkItem(async j =>
                {
                    var actorReference = new ActorReference { ActorId = ActorId.CreateRandom() };
                    try
                    {
                        await manager.OnSubscribedAsync("Key" + (int) j % 5, new ActorReferenceWrapper(actorReference),
                            "MessageType");
                    }
                    catch (NullReferenceException)
                    {
                        hasCrashed = true;
                    }
                    catch (IndexOutOfRangeException)
                    {
                        hasCrashed = true;
                    }
                    finally
                    {
                        if ((int)j == attempts - 1)
                        {
                            mr2.Set();
                        }
                    }
                }, i);


            }

            mr.Set();

            Assert.IsTrue(mr2.WaitOne(TimeSpan.FromSeconds(10)), "Failed to run within time limits.");
            Assert.IsFalse(hasCrashed, "Should not crash.");
        }
    }
}
