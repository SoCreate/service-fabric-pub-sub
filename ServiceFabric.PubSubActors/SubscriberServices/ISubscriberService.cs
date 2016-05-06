using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using ServiceFabric.PubSubActors.Interfaces;

namespace ServiceFabric.PubSubActors.SubscriberServices
{
	/// <summary>
	/// Defines a common interface for all Subscriber Services. 
	/// </summary>
	[ServiceContract]
	public interface ISubscriberService : IService
    {
		/// <summary>
		/// Receives a published message.
		/// </summary>
		/// <param name="message"></param>
		/// <returns></returns>
		[OperationContract(IsOneWay = true)]
		Task ReceiveMessageAsync(MessageWrapper message);
	}
}
