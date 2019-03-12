using System.ServiceModel;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Services.Remoting;

namespace Common.DataContracts
{
	[ServiceContract]
	public interface IPublishingStatelessService : IService
	{
		[OperationContract]
		Task<string> PublishMessageOneAsync();

        [OperationContract]
        Task<string> PublishMessageTwoAsync();
    }
}
