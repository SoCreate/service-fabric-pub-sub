using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;
using SoCreate.ServiceFabric.PubSub.State;

namespace SoCreate.ServiceFabric.PubSub.Subscriber
{
    /// <summary>
    /// Defines a common interface for all Subscriber Services.
    /// </summary>
    public interface ISubscriberService : IService
    {
        /// <summary>
        /// Receives a published message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        Task ReceiveMessageAsync(MessageWrapper message);
    }
}
