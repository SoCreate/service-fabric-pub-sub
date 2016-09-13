using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Generic message format. Contains message CLR type (full name) and serialized payload. If you know the Message Type you can deserialize 
	/// the payload into that object.
	/// </summary>
	[DataContract]
	public partial class MessageWrapper
	{
		/// <summary>
		/// Indicates whether this message was relayed.
		/// </summary>
		[DataMember]
		public bool IsRelayed { get; set; }

		/// <summary>
		/// CLR Type Full Name of serialized payload.
		/// </summary>
		[DataMember]
		public string MessageType { get; set; }

		/// <summary>
		/// Serialized object.
		/// </summary>
		[DataMember]
		public string Payload { get; set; }
	}

    public partial class MessageWrapper
    {
        /// <summary>
        /// Convert the provided <paramref name="message"/> into a <see cref="MessageWrapper"/>
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static MessageWrapper CreateMessageWrapper(object message)
        {
            var wrapper = new MessageWrapper
            {
                MessageType = message.GetType().FullName,
                Payload = JsonConvert.SerializeObject(message),
            };
            return wrapper;
        }
    }
}