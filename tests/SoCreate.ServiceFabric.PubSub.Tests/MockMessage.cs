namespace SoCreate.ServiceFabric.PubSub.Tests
{
    public class MockMessage
    {
        public string SomeValue { get; set; }

    }

    public class MockMessageSpecialized : MockMessage
    {
        public string SomeOtherValue { get; set; }

    }
}
