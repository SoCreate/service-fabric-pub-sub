using Microsoft.ServiceFabric.Actors;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Tests
{
    [TestClass]
    public class GivenServiceReference
    {
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithMatchingPayload_ThenReturnsTrue()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name=Customer1");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = serviceRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }

        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithUnmatchingPayload_ThenReturnsFalse()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name=Customer1");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer2"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = serviceRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsFalse(shouldDeliver);
        }

        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayload_ThenReturnsFalse()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name=Customer1");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer2"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsFalse(shouldDeliver);
        }
    }
}
