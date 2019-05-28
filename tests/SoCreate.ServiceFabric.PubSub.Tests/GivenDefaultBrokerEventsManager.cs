using Microsoft.ServiceFabric.Actors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoCreate.ServiceFabric.PubSub.Events;
using SoCreate.ServiceFabric.PubSub.State;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenDefaultBrokerEventsManager
    {
        [TestMethod]
        public async Task WhenInsertingConcurrently_ThenAllCallbacksAreMadeCorrectly()
        {
            int callCount = 0;
            var manager = new DefaultBrokerEventsManager();
            manager.Subscribed += (s, e, t) =>
            {
                lock (manager)
                {
                    callCount++;
                }

                return Task.CompletedTask;

            };
            const int attempts = 20;
            var tasks = new List<Task>(attempts);

            for (int i = 0; i < attempts; i++)
            {
                var actorReference = new ActorReference{ ActorId =  ActorId.CreateRandom() };
                tasks.Add(manager.OnSubscribedAsync("Key", new ActorReferenceWrapper(actorReference), "MessageType"));
            }

            await Task.WhenAll(tasks);

            Assert.AreEqual(attempts, callCount);
        }
    }
}
