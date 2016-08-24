namespace Common.DataContracts
{
    public abstract class DataContract
    {
        public string Payload { get; set; }

        protected DataContract()
        {
        }

        protected DataContract(int payloadSize)
        {
            Payload = new string('a', payloadSize);
        }
    }
}