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
		/// Converts the provided string representation into a object.
		/// </summary>
		/// <param name="serializedData"></param>
		/// <returns></returns>
		object Deserialize(string serializedData);
	}
}