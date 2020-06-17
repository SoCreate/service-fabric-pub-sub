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
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "Customer1");
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
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "Customer1");
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
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name" , "Customer1");
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
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayload_ThenReturnsTrue()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "Customer1");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }

        /// Testing Wild Card Support 
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithMatchingPayloadWithWildCard_ThenReturnsTrue()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "*");
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
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithWildCard_ThenReturnsTrue()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "*");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }



        /// Testing Starting Character Wild Card Support
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithMatchingPayloadWithStartingWildCard_ThenReturnsTrue()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "*1");
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
        public void WhenDeterminingShouldDeliverMessageToServiceWithUnmatchingPayloadWithStartingWildCard__ThenReturnsFalse()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "*1");
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
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithStartingWildCard__ThenReturnsFalse()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "*1");
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
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithStartingWildCard__ThenReturnsTrue()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "*1");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }

        /// Testing Ending Character Wild Card Support
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithMatchingPayloadWithEndingWildCard_ThenReturnsTrue()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "Customer*");
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
        public void WhenDeterminingShouldDeliverMessageToServiceWithUnmatchingPayloadWithEndingWildCard__ThenReturnsFalse()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "Supplier*");
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
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithEndingWildCard__ThenReturnsFalse()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "Supplier*");
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
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithEndingWildCard__ThenReturnsTrue()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "Customer*");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }

        /// Testing Multiple Character Wild Card Support
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToServiceWithMatchingPayloadWithMultipleWildCard_ThenReturnsTrue()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "*Customer*");
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
        public void WhenDeterminingShouldDeliverMessageToServiceWithUnmatchingPayloadWithMultipleWildCard__ThenReturnsFalse()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "Customer.Name", "*Supplier*");
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
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithMultipleWildCard__ThenReturnsFalse()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "*Supplier*");
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
        [TestMethod]
        public void WhenDeterminingShouldDeliverMessageToActorWithUnmatchingPayloadWithMultipleWildCard__ThenReturnsTrue()
        {
            var actorRef = new ActorReferenceWrapper(new ActorReference { ActorId = ActorId.CreateRandom() }, "Customer.Name", "*Customer*");
            var messageWrapper = new
            {
                Customer = new
                {
                    Name = "Customer1"
                }
            }.CreateMessageWrapper();

            bool shouldDeliver = actorRef.ShouldDeliverMessage(messageWrapper);
            Assert.IsTrue(shouldDeliver);
        }
    }
}
