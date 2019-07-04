using System;

namespace SoCreate.ServiceFabric.PubSubDemo.SampleEvents
{
    public class SampleEvent
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public SampleEvent()
        {
            Id = Guid.NewGuid();
        }
    }
    
    public class SampleUnorderedEvent
    {
        public Guid Id { get; set; }

        public string Message { get; set; }

        public SampleUnorderedEvent()
        {
            Id = Guid.NewGuid();
        }
    }
}