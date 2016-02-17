using System.Runtime.Serialization;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// Generic message format. Contains message CLR type (full name) and serialized payload. If you know the Message Type you can deserialize 
	/// the payload into that object.
	/// </summary>
	[DataContract]
	public class MessageWrapper
	{
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
}