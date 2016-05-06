using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common.DataContracts;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Services.Remoting.Client;
using PublishingActor.Interfaces;
using ServiceFabric.PubSubActors.Interfaces;
using SubscribingActor.Interfaces;

namespace TestClient
{
	class Program
	{
		static void Main(string[] args)
		{
			var applicationName = "fabric:/MyServiceFabricApp";
			var serviceName = $"{applicationName}/PublishingStatelessService";
			var pubActor = GetPublishingActor(applicationName);
			var pubService = GetPublishingService(new Uri(serviceName));

			RegisterSubscribers(applicationName);


			while (true)
			{
				Console.Clear();
				Console.WriteLine("Hit 1 to send message one, using an Actor.");
				Console.WriteLine("Hit 2 to send message one, using a Service");
				Console.WriteLine("Hit escape to exit.");
				var key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.D1:
						{
							pubActor.PublishMessageOneAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message one from Actor!");
						}
						break;
					case ConsoleKey.D2:
						{
							pubService.PublishMessageOneAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message one from Service!");
						}
						break;

					case ConsoleKey.Escape:
						return;
				}

			}

		}

		private static IPublishingStatelessService GetPublishingService(Uri serviceName)
		{
			IPublishingStatelessService pubService = null;

			while (pubService == null)
			{
				try
				{
					pubService = ServiceProxy.Create<IPublishingStatelessService>(serviceName);
				}
				catch
				{
					Thread.Sleep(200);
				}
			}
			return pubService;
		}

		private static IPublishingActor GetPublishingActor(string applicationName)
		{
			IPublishingActor pubActor = null;
			var actorId = new ActorId("PubActor");

			while (pubActor == null)
			{
				try
				{
					pubActor = ActorProxy.Create<IPublishingActor>(actorId, applicationName);
				}
				catch
				{
					Thread.Sleep(200);
				}
			}
			return pubActor;
		}

		private static void RegisterSubscribers(string applicationName)
		{
			for (int i = 0; i < 4; i++)
			{
				var actorId = new ActorId("SubActor" + i.ToString("0000"));

				ISubscribingActor subActor = null;
				while (subActor == null)
				{
					try
					{
						subActor = ActorProxy.Create<ISubscribingActor>(actorId, applicationName, nameof(ISubscribingActor));

						if (i%2 == 0)
						{
							subActor.RegisterAsync().GetAwaiter().GetResult();
						}
						else
						{
							subActor.RegisterWithRelayAsync().GetAwaiter().GetResult();
						}
					}
					catch
					{
						subActor = null;
						Thread.Sleep(200);
					}
				}
			}
		}
	}
}
