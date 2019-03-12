using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using ServiceFabric.PubSubActors.State;

namespace ServiceFabric.PubSubActors.Tests
{
    [TestClass]
    public class GivenServiceReferenceWrapper
    {
        [TestMethod]
        public void WhenDeserializing_ThenHashingHelperIsNotNull()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "A=B");
            var serializer = new DataContractSerializer(typeof(ServiceReferenceWrapper));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, serviceRef);
                stream.Position = 0;

                var cloneServiceRef = (ServiceReferenceWrapper)serializer.ReadObject(stream);
                var hashingHelper = typeof(ServiceReferenceWrapper)
                    .GetProperty("HashingHelper", BindingFlags.Instance | BindingFlags.NonPublic)
                    .GetValue(cloneServiceRef);

                Assert.IsInstanceOfType(hashingHelper, typeof(IHashingHelper));
            }
        }


        [TestMethod]
        public void WhenDeserializing_ThenRoutingKeyIsPreserved()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "A=B");
            var serializer = new DataContractSerializer(typeof(ServiceReferenceWrapper));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, serviceRef);
                stream.Position = 0;

                var cloneServiceRef = (ServiceReferenceWrapper)serializer.ReadObject(stream);
                Assert.AreEqual("A=B", cloneServiceRef.RoutingKey);
            }
        }

        [TestMethod]
        public void WhenDeserializing_ThenNameIsPreserved()
        {
            var serviceRef = new ServiceReferenceWrapper(new ServiceReference(), "A=B");
            var serializer = new DataContractSerializer(typeof(ServiceReferenceWrapper));
            using (var stream = new MemoryStream())
            {
                serializer.WriteObject(stream, serviceRef);
                stream.Position = 0;

                var cloneServiceRef = (ServiceReferenceWrapper)serializer.ReadObject(stream);
                Assert.AreEqual(serviceRef.Name, cloneServiceRef.Name);
            }
        }
    }
}
