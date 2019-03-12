using System;
using System.Collections.Generic;
using System.Fabric;
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
using ServiceFabric.PubSubActors.SubscriberServices;
using SubscribingActor.Interfaces;
using SubscribingStatelessService;

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
			var subStateless = GetSubStatelessProxy();
			RegisterSubscribers(applicationName);

			while (true)
			{
				Console.Clear();
				Console.WriteLine("Hit 1 to send a message one to the BrokerActor, using an Actor.");
				Console.WriteLine("Hit 2 to send a message one to the BrokerActor, using a Service");

				Console.WriteLine("Hit 3 to send a message one to the BrokerService, using an Actor.");
				Console.WriteLine("Hit 4 to send a message one to the BrokerService, using a Service");

				Console.WriteLine("Hit 5 to unsubscribe SubscribingStatelessService from message type PublishedMessageOne");
				Console.WriteLine("Hit 6 to subscribe SubscribingStatelessService to message type PublishedMessageOne");

				Console.WriteLine("Hit escape to exit.");
				var key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.D1:
						{
							pubActor.PublishMessageOneAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message one from Actor Broker Actor!");
						}
						break;
					case ConsoleKey.D2:
						{
							pubService.PublishMessageOneAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message one from Service using Broker Actor!");
						}
						break;

					case ConsoleKey.D3:
						{
							pubActor.PublishMessageTwoAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message two from Actor using Broker Service!");
						}
						break;
					case ConsoleKey.D4:
						{
							pubService.PublishMessageTwoAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message two from Service using Broker Service!");
						}
						break;
					case ConsoleKey.D5:
						{
							subStateless.UnregisterAsync().ConfigureAwait(false).GetAwaiter().GetResult();
							Console.WriteLine("Unsubscribed SubscribingStatelessService from message type PublishedMessageOne");
						}
						break;
					case ConsoleKey.D6:
						{
							subStateless.RegisterAsync().ConfigureAwait(false).GetAwaiter().GetResult();
							Console.WriteLine("Subscribed SubscribingStatelessService from message type PublishedMessageOne");
						}
						break;
					case ConsoleKey.Escape:
						return;
				}

			}

		}

		private static ISubscribingStatelessService GetSubStatelessProxy()
		{
			ISubscribingStatelessService proxy = null;

			while (proxy == null)
			{
				try
				{
					proxy = ServiceProxy.Create<ISubscribingStatelessService>(new Uri("fabric:/MyServiceFabricApp/SubscribingStatelessService"),
						listenerName: "StatelessFabricTransportServiceRemotingListener");
				}
				catch
				{
					Thread.Sleep(200);
				}
			}
			return proxy;
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

						if (i % 3 == 0)
						{
							subActor.RegisterAsync().GetAwaiter().GetResult();
						}
						else if (i % 3 == 1)
						{
							subActor.RegisterWithRelayAsync().GetAwaiter().GetResult();
						}
						else
						{
							subActor.RegisterWithBrokerServiceAsync().GetAwaiter().GetResult();
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
