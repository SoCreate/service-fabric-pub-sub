using System;
using Newtonsoft.Json;

namespace ServiceFabric.PubSubActors.Interfaces
{
	/// <summary>
	/// The default serializer to use for <see cref="MessageWrapper.Payload"/>
	/// </summary>
	public class DefaultPayloadSerializer : IPayloadSerializer
	{
		/// <inheritdoc />
		public string Serialize(object payload)
		{
			return JsonConvert.SerializeObject(payload);
		}

		/// <inheritdoc />
		public object Deserialize(string serializedData)
		{
			return JsonConvert.DeserializeObject(serializedData);
		}

	    /// <inheritdoc />
	    public object Deserialize(string serializedData, Type type)
	    {
	        return JsonConvert.DeserializeObject(serializedData, type);
	    }

        /// <inheritdoc />
        public TResult Deserialize<TResult>(string serializedData)
	    {
	        return JsonConvert.DeserializeObject<TResult>(serializedData);
        }
    }

	/// <summary>
	/// Converts objects to strings and back again.
	/// </summary>
	public interface IPayloadSerializer
	{
		/// <summary>
		/// Converts the provided object into a string representation.
		/// </summary>
		/// <param name="payload"></param>
		/// <returns></returns>
		string Serialize(object payload);

		/// <summary>
		/// Converts the provided string representation into an object.
		/// </summary>
		/// <param name="serializedData"></param>
		/// <returns></returns>
		object Deserialize(string serializedData);

	    /// <summary>
	    /// Converts the provided string representation into an object of type <paramref name="type"/>.
	    /// </summary>
	    /// <param name="serializedData"></param>
	    /// <param name="type"></param>
	    /// <returns></returns
	    object Deserialize(string serializedData, Type type);

        /// <summary>
        /// Converts the provided string representation into an object of type <typeparamref name="TResult"/>.
        /// </summary>
        /// <param name="payload"></param>
        /// <returns></returns
        TResult Deserialize<TResult>(string payload);
    }
}