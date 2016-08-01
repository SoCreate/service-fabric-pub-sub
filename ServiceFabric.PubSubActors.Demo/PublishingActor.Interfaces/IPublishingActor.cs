using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;

namespace PublishingActor.Interfaces
{
	public interface IPublishingActor : IActor
	{
		Task<string> PublishMessageOneAsync();

        Task<string> PublishMessageTwoAsync();
    }
}
