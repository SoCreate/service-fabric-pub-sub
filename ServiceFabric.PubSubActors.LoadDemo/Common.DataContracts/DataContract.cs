using System;

namespace Common.DataContracts
{
    public class DataContract
    {
        public Guid Id { get; set; }

        public string Payload { get; set; }

        protected DataContract()
        {
            Id = Guid.NewGuid();
        }

        protected DataContract(int payloadSize) : this()
        {
            Payload = new string('a', payloadSize);
        }
    }
}