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
