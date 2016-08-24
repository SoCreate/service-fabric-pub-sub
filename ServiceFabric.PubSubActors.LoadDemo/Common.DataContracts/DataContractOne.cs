namespace Common.DataContracts
{
    public class DataContractOne : DataContract
    {
        public DataContractOne()
        {
        }

        public DataContractOne(int payloadSize) : base(payloadSize)
        {
        }
    }
}