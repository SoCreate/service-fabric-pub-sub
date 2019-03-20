using System;
using System.Reflection;
using System.Runtime.Serialization;
using SoCreate.ServiceFabric.PubSub.Helpers;

namespace SoCreate.ServiceFabric.PubSub.State
{
	/// <summary>
	/// Generic message format. Contains message CLR type (full name) and serialized payload. If you know the Message Type you can deserialize
	/// the payload into that object.
	/// </summary>
	[DataContract]
	public class MessageWrapper
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
	    /// CLR Type Assembly Name of serialized payload.
	    /// </summary>
	    [DataMember]
	    public string Assembly { get; set; }

        /// <summary>
        /// Serialized object.
        /// </summary>
        [DataMember]
		public string Payload { get; set; }
	}

    public static class MessageWrapperExtensions
    {
		/// <summary>
		/// Gets or sets the <see cref="IPayloadSerializer"/> to use when setting the <see cref="MessageWrapper.Payload"/>.
		/// Defaults to <see cref="DefaultPayloadSerializer"/> which uses Json.Net.
		/// </summary>
		public static IPayloadSerializer PayloadSerializer { get; set; } = new DefaultPayloadSerializer();

	    /// <summary>
	    /// Convert the provided <paramref name="message"/> into a <see cref="MessageWrapper"/>
	    /// </summary>
	    /// <param name="message"></param>
	    /// <returns></returns>
	    public static MessageWrapper CreateMessageWrapper(this object message)
        {
            var messageType = message.GetType();
            var wrapper = new MessageWrapper
            {
                MessageType = messageType.FullName,
                Assembly = messageType.Assembly.FullName,
                Payload = (PayloadSerializer ?? new DefaultPayloadSerializer()).Serialize(message),
            };
            return wrapper;
        }

        /// <summary>
        /// Convert the provided <paramref name="messageWrapper"/> into an object of type <typeparamref name="TResult"/>
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public static TResult CreateMessage<TResult>(this MessageWrapper messageWrapper)
        {
            var message = (PayloadSerializer ?? new DefaultPayloadSerializer()).Deserialize<TResult>(messageWrapper.Payload);
            return message;
        }

        /// <summary>
        /// Convert the provided <paramref name="messageWrapper"/> into an object of type <see cref="MessageWrapper.MessageType"/>
        /// </summary>
        /// <param name="messageWrapper"></param>
        /// <returns></returns>
        public static object CreateMessage(this MessageWrapper messageWrapper)
        {
            var type = Type.GetType(messageWrapper.MessageType, false);
            if (type == null)
            {
                type = Assembly.Load(messageWrapper.Assembly).GetType(messageWrapper.MessageType, true);
            }
            var message = (PayloadSerializer ?? new DefaultPayloadSerializer()).Deserialize(messageWrapper.Payload, type);
            return message;
        }
    }
}