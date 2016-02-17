using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.ServiceFabric.Actors;
using PublishingActor.Interfaces;
using ServiceFabric.PubSubActors.Interfaces;
using SubscribingActor.Interfaces;

namespace TestClient
{
	class Program
	{
		static void Main(string[] args)
		{
			string applicationName = "fabric:/MyServiceFabricApp";
			var actorId = new ActorId("PubActor");
			IPublishingActor pubActor = null;

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

			
			RegisterSubscribers(applicationName);
			

			while (true)
			{
				Console.Clear();
				Console.WriteLine("Hit 1 to send message one, or hit escape to exit.");
				var key = Console.ReadKey(true);

				switch (key.Key)
				{
					case ConsoleKey.D1:
						{
							pubActor.PublishMessageOneAsync().GetAwaiter().GetResult();
							Console.WriteLine("Sent message one!");
						}
						break;

					case ConsoleKey.Escape:
						return;
				}

			}

		}

		private static void RegisterSubscribers(string applicationName)
		{
			for (int i = 0; i < 10; i++)
			{
				var actorId = new ActorId("SubActor" + i.ToString("0000"));

				ISubscribingActor subActor = null;
				while (subActor == null)
				{
					try
					{
						subActor = ActorProxy.Create<ISubscribingActor>(actorId, applicationName, nameof(ISubscribingActor));
						subActor.RegisterAsync().GetAwaiter().GetResult();
					}
					catch
					{
						Thread.Sleep(200);
					}
				}
			}
		}
	}
}
