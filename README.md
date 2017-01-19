# Service Fabric Pub/Sub (Not just for Actors anymore!)
Do you want to create an Event Driven Architecture while using Azure Service Fabric?
Do you need to reliably broadcast messages between Actors and Services?
This code will help you do that.
It supports both Actors and Services as publishers and subscribers.

It uses extension methods to
- Actor
- StatelessService
- StatefulService

##Release notes:

- 4.8.1 BrokerService.Subscribers is now protected, not private
- 4.8.0 upgraded to new SDK (2.4.145) 
- 4.7.1 merged pull request by Sterlingg that fixes Dispose issue
- 4.7.0 upgraded to new SDK (2.3.311)
- 4.6.1 merged pr by kelvintmv
- 4.6.0 upgraded to new SDK and packages (2.3.301) 
- 4.5.4 fixed unregister issue found by schernets
- 4.5.0 updated nuget packages (new SDK), moving from extension methods to injectable helpers for test support.
- 4.4.13 fixed memory leak.
- 4.4.0 improved BrokerService throughput. Added load test demo app.
- 4.2.0 added BrokerService as counterpart of BrokerActor, so you can use your favorite programming model.
- 4.0.4 moved from dnu to dotnet core project
- 4.0.3 updated nuget packages (new SDK)

## Nuget:
https://www.nuget.org/packages/ServiceFabric.PubSubActors
https://www.nuget.org/packages/ServiceFabric.PubSubActors.Interfaces (for Actor interfaces)
https://www.nuget.org/packages/ServiceFabric.PubSubActors.PackagedBrokerService (PREVIEW) (add BrokerService to existing Service Fabric Application)

## Getting started
(This is the short version.)

1. Add Nuget package 'ServiceFabric.PubSubActors.PackagedBrokerService' (PREVIEW) to your existing Service Fabric application. 
2. Add Nuget package 'ServiceFabric.PubSubActors' to your Actor or Service project.
3. Publish a message

	3.1. From an Actor:  
	``` javascript
	using ServiceFabric.PubSubActors.PublisherActors;
	[..]
	this.PublishMessageToBrokerServiceAsync(..);  	//use any JSON serializable object as a message
	```

	3.2. From a Service:  
	``` javascript
	using ServiceFabric.PubSubActors.PublisherServices;
	[..]
	this.PublishMessageToBrokerServiceAsync(..);	//use any JSON serializable object as a message
	```

4. Subscribe to messages

	4.1. From an Actor: implement ISubscriberActor (like described below)
	
	4.2. From a Service : implement ISubscriberService and add the 'SubscriberCommunicationListener' (like described below)

That's it. Actors and Services can be both subscribers and publishers.

#
Now the long version:

## Introduction

Using this package you can reliably send messages from Publishers (Actors/Services) to many Subscribers (Actors/Services). 
This is done using an intermediate, which is the BrokerActor or BrokerService.
Add this package to all Reliable Actor & Service projects that participate in the pub/sub messaging.
Add the package 'ServiceFabric.PubSubActors.Interfaces' to all (*ReliableActor).Interfaces projects.


|    publisher  |     broker    |subscriber|
| ------------- |:-------------:| -----:|
|[Publishing Actor]||
||[BrokerService]|
|||[Subscribing Actors]|
|||[Subscribing Services]|
|[Publishing Service]||
||[BrokerService]|
|||[Subscribing Services]|
|||[Subscribing Actors]|

Or if you like using Actors, you can use the BrokerActor:

|    publisher  |     broker    |subscriber|
| ------------- |:-------------:| -----:|
|[Publishing Actor]||
||[BrokerActor]|
|||[Subscribing Actors]|
|||[Subscribing Services]|
|[Publishing Service]||
||[BrokerActor]|
|||[Subscribing Services]|
|||[Subscribing Actors]|

For large scale messaging with many subscribers you can use a layered approach using RelayBrokerActors:

|    publisher  |     broker    | relaybrokers | subscriber|
| ------------- |:-------------:|:-------------:| -----:|
|[Publishing Actor]||
||[BrokerActor]|
|||[RelayBrokerActors]|
||||[Subscribing Actors]|
||||[Subscribing Services]|
|[Publishing Service]||
||[BrokerActor]|
|||[RelayBrokerActors]|
||||[Subscribing Services]|
||||[Subscribing Actors]|
*note: only message subscriptions are different here, publishing still happens using the default broker*

## How to use:

### Create a Custom BrokerActor type
*Actors of this type will be used to register subscribers to, and every instance will publish one type of message.*

Add a new Stateful Reliable Actor project. Call it 'PubSubActor' (optional).
Add Nuget package 'ServiceFabric.PubSubActors' to the 'PubSubActor' project
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to the 'PubSubActor.Interfaces' project.
Replace the code of PubSubActor with the following code:

```javascript
[ActorService(Name = nameof(IBrokerActor))] //required because of interface operations
internal class PubSubActor : ServiceFabric.PubSubActors.BrokerActor, Interfaces.IBrokerActor
{
	public PubSubActor() 
	{
        //optional: provide a logging callback
		ActorEventSourceMessageCallback = message => ActorEventSource.Current.ActorMessage(this, message);
	}
}
```

#### Or, to use a Custom BrokerService:
Add a new Stateful Reliable Service project. Call it 'PubSubService' (optional).
Add Nuget package 'ServiceFabric.PubSubActors' to the 'PubSubActor' project
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to the project.
Replace the code of PubSubService with the following code:
```javascript
internal sealed class PubSubService : BrokerService
{
    public PubSubService(StatefulServiceContext context)
       : base(context)
    {
	//optional: provide a logging callback
	ServiceEventSourceMessageCallback = message => ServiceEventSource.Current.ServiceMessage(this, message);
    }
}
```

### Optional, for 'Large Scale Messaging' using Broker Actors: Add a RelayBrokerActor type to your existing BrokerActor (Not in combination with the BrokerService)
*Actors of this type will be used to relay messges from a BrokerActor, and relay it to registered subscribers, and every instance will publish one type of message.*

Add a new Stateful Reliable Actor project. Call it 'PubkSubRelayActor'.
Add Nuget package 'ServiceFabric.PubSubActors' to the 'PubkSubRelayActor' project
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to the 'PubkSubRelayActor.Interfaces' project.
Replace the code of PubkSubRelayActor with the following code:

```javascript
	[StatePersistence(StatePersistence.Persisted)]
	[ActorService(Name = nameof(IRelayBrokerActor))]
	internal class PubkSubRelayActor : RelayBrokerActor, IPubkSubRelayActor
	{
		public PubkSubRelayActor()
		{
			//optional: provide a logging callback
			ActorEventSourceMessageCallback = message => ActorEventSource.Current.ActorMessage(this, message);
		}
	}
```

### Add a shared data contracts library
*Add a common datacontracts library for shared messages*

Add a new Class Library project, call it 'DataContracts', and add these sample message contracts:
```javascript
[DataContract]
public class PublishedMessageOne
{
	[DataMember]
	public string Content { get; set; }
}
[DataContract]
public class PublishedMessageTwo
{
	[DataMember]
	public string Content { get; set; }
}
```

### Subscribing to messages using Actors
*Create a sample Actor that implements 'ISubscriberActor', to become a subscriber to messages.*
In this example, the Actor called 'SubscribingActor' subscribes to messages of Type 'PublishedMessageOne'.

Add a Reliable Stateless Actor project called 'SubscribingActor'.
Add Nuget package 'ServiceFabric.PubSubActors' to the 'SubscribingActor' project
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to the 'SubscribingActor.Interfaces' project.
Add a project reference to the shared data contracts library ('DataContracts').

Go to the SubscribingActor.Interfaces project, open the file 'ISubscribingActor' and replace the contents with this code:
**notice this implements ISubscriberActor from the package 'ServiceFabric.PubSubActors.Interfaces' which adds a Receive method. The additional methods are to enable this actor to be manipulated from the outside.**
```javascript
public interface ISubscribingActor : ISubscriberActor
	{
		// allow external callers to manipulate register/unregister on this sample actor:
		//for regular messaging:
		Task RegisterAsync(); 
		Task UnregisterAsync();
		//for relayed messaging:
		Task RegisterWithRelayAsync();
		Task UnregisterWithRelayAsync();
		//for service broker messaging:
		Task RegisterWithBrokerServiceAsync();
		Task UnregisterWithBrokerServiceAsync();
	}
```

Open the file 'SubscribingActor.cs' and replace the contents with the code below.
**notice that this Actor now implements 'ISubscriberActor' indirectly.**
```javascript
using ServiceFabric.PubSubActors.SubscriberActors;

[ActorService(Name = nameof(ISubscribingActor))]
[StatePersistence(StatePersistence.None)]
internal class SubscribingActor : Actor, ISubscribingActor
{
	private const string WellKnownRelayBrokerId = "WellKnownRelayBroker";
	//register to the default broker:
	public Task RegisterAsync()
	{
		return this.RegisterMessageTypeAsync(typeof(PublishedMessageOne)); //register as subscriber for this type of messages
	}
	public Task UnregisterAsync()
	{
		return this.UnregisterMessageTypeAsync(typeof(PublishedMessageOne), true); //unregister as subscriber for this type of messages
	}
	
	//for large scale messaging, register a relay to the original broker, and register as a subscriber to that relay:
	public Task RegisterWithRelayAsync()
	{
		//register as subscriber for this type of messages at the relay broker
		//using the default Broker for the message type as source for the relay broker
		return this.RegisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null); 
	}
	public Task UnregisterWithRelayAsync()
	{
		//unregister as subscriber for this type of messages at the relay broker
		return this.UnregisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null,  true); 
	}
	
 	public Task RegisterWithBrokerServiceAsync()
        {
            return this.RegisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo));
        }

        public Task UnregisterWithBrokerServiceAsync()
        {
            return this.UnregisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo), true);
        }

	public Task ReceiveMessageAsync(MessageWrapper message)
	{
		var payload = this.Deserialize<PublishedMessageOne>(message);
		ActorEventSource.Current.ActorMessage(this, $"Received message: {payload.Content}");
		//TODO: handle message
		return Task.FromResult(true);
	}
}
```
You can now register 'SubscriberActor' by calling 'RegisterAsync', to start receiving messages from the PubSubActor using 'ReceiveMessageAsync'.

You can now register 'SubscriberActor' by calling 'RegisterWithRelayAsync', to start receiving messages from the PubSubRelayActor instance "WellKnownRelayBroker", using 'ReceiveMessageAsync'.

You could so by adding this code to the Program.Main method, call it from within the Actor itself, or call it from the outside using the interface "ISubscribingActor"

```javascript
//from outside the Actor, using the custom interface ISubscribingActor
ActorId actorId = new ActorId("SubActor");
string applicationName = "fabric:/MyServiceFabricApp"; //replace with your application name
ISubscriberActor subActor = ActorProxy.Create<ISubscribingActor>(actorId, applicationName, nameof(ISubscribingActor));
subActor.RegisterAsync().GetAwaiter().GetResult();
```

### Subscribing to messages using Services
*Create a sample Service that implements 'ISubscriberService', to become a subscriber to messages.*
In this example, the Service called 'SubscribingStatefulService' subscribes to messages of Type 'PublishedMessageOne'.

Add a Reliable Stateful Service project called 'SubscribingStatefulService'.
Add Nuget package 'ServiceFabric.PubSubActors'.
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces'.
Add a project reference to the shared data contracts library ('DataContracts').

Now open the file SubscribingStatefulService.cs in the project 'SubscribingStatefulService' and replace the contents with this code:
(Implement 'ServiceFabric.PubSubActors.SubscriberServices.ISubscriberService' and self-register.)

```javascript
using ServiceFabric.PubSubActors.Interfaces;
using ServiceFabric.PubSubActors.SubscriberServices;

internal sealed class SubscribingStatefulService : StatefulService, ISubscriberService
{
	public SubscribingStatefulService(StatefulServiceContext serviceContext) : base(serviceContext)
	{
	}

	public SubscribingStatefulService(StatefulServiceContext serviceContext, IReliableStateManagerReplica 						reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
	{
	}
		
	protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
	{
		//add SubscriberCommunicationListener for receiving published messages.
		yield return new ServiceReplicaListener(p => new SubscriberCommunicationListener(this, p), "StatefulSubscriberCommunicationListener);
	}

	protected override Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
	{
		return RegisterAsync();
	}
		
	public async Task RegisterAsync()
	{	
		//register with BrokerActor:	
	    	await this.RegisterMessageTypeAsync(typeof(PublishedMessageOne));
	    	//register with BrokerService:
            	await this.RegisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo));
	}

	public async Task UnregisterAsync()
	{
		//unregister with BrokerActor:	
		await this.UnregisterMessageTypeAsync(typeof(PublishedMessageOne), true);
		//unregister with BrokerService:
            	await this.UnregisterMessageTypeWithBrokerServiceAsync(typeof(PublishedMessageTwo), true);
	}

	//receives published messages:
	public Task ReceiveMessageAsync(MessageWrapper message)
	{
		var payload = this.Deserialize<PublishedMessageOne>(message);
		ServiceEventSource.Current.ServiceMessage(this, $"Received message: {payload.Content}");
		//TODO: handle message
		return Task.FromResult(true);
	}
}
```
You can also call 'RegisterAsync' to make the service register itself as subscriber, after adding it to a custom interface, provided 'OnOpenAsync' has been called first.
#### Subscribing to a RelayBroker

For large scale messaging support, subscribe to a (Well known) Relay Broker. Follow the steps from above, but use this code in the service:
``` javascript
internal sealed class SubscribingToRelayStatefulService : StatefulService, ISubscriberService
{
	private const string WellKnownRelayBrokerId = "WellKnownRelayBroker";
	public SubscribingToRelayStatefulService(StatefulServiceContext serviceContext) : base(serviceContext)
	{
	}
	public SubscribingToRelayStatefulService(StatefulServiceContext serviceContext, IReliableStateManagerReplica reliableStateManagerReplica) : base(serviceContext, reliableStateManagerReplica)
	{
	}

	protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
	{
		yield return new ServiceReplicaListener(p => new SubscriberCommunicationListener(this, p), "RelayStatefullSubscriberCommunicationListener");
	}

	protected override async Task OnOpenAsync(ReplicaOpenMode openMode, CancellationToken cancellationToken)
	{
		await RegisterAsync();
		//do other stuff
	}

	public Task RegisterAsync()
	{
		return this.RegisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null);
	}

	public Task UnregisterAsync()
	{
		return this.UnregisterMessageTypeWithRelayBrokerAsync(typeof(PublishedMessageOne), new ActorId(WellKnownRelayBrokerId), null, true);
	}

	public Task ReceiveMessageAsync(MessageWrapper message)
	{
		var payload = this.Deserialize<PublishedMessageOne>(message);
		ServiceEventSource.Current.ServiceMessage(this, $"Received message: {payload.Content}");
		//TODO: handle message
		return Task.FromResult(true);
	}
}
```



### Publishing messages from Actors
*Create a sample Actor that publishes messages.*
In this example, the Actor called 'PublishingActor' publishes messages of Type 'PublishedMessageOne'.

In this example, the Publisher Actor publishes messages of Type 'PublishedMessageOne'.
Add a Reliable Stateless Actor project called 'PublishingActor'.
Add Nuget package 'ServiceFabric.PubSubActors' to 'PublishingActor'.
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to 'PublishingActor.Interfaces'.
Add a project reference to the shared data contracts library ('DataContracts').

Go to the project 'PublishingActor.Interfaces' and open the file IPublishingActor.cs. 
Replace the contents with the code below, to allow external callers to trigger a publish action (not required, Actors can decide for themselves too):

```javascript
public interface IPublishingActor : IActor
{
	//enables external callers to trigger a publish action, not required for functionality
	Task<string> PublishMessageOneAsync();
	Task<string> PublishMessageTwoAsync();
}
```

Now open the file PublishingActor.cs in the project 'PublishingActor' and replace the contents with this code:

```javascript
using ServiceFabric.PubSubActors.PublisherActors;
[StatePersistence(StatePersistence.None)]
internal class PublishingActor : Actor, IPublishingActor
{
	//publish to BrokerActor
	async Task<string> IPublishingActor.PublishMessageOneAsync()
	{
		ActorEventSource.Current.ActorMessage(this, "Publishing Message");
		await this.PublishMessageAsync(new PublishedMessageOne {Content = "Hello PubSub World, from Actor!"});
		return "Message published to Broker Actor";
	}
	
	//publish to BrokerService
	async Task<string> IPublishingActor.PublishMessageTwoAsync()
        {
            ActorEventSource.Current.ActorMessage(this, "Publishing Message");

            await this.PublishMessageToBrokerServiceAsync(new PublishedMessageTwo { Content = "Hello PubSub World, from Actor, using 	Broker Service!" });
            return "Message published to broker service";
        }
}
```
You can now ask the PublishingActor to publish a message to subscribers:

```javascript
string applicationName = "fabric:/MyServiceFabricApp";  //replace with your application name
var actorId = new ActorId("PubActor");
pubActor = ActorProxy.Create<IPublishingActor>(actorId, applicationName);
pubActor.PublishMessageOneAsync()
```

### Publishing messages from Services
*Create a sample Service that publishes messages.*
In this example, the Service called 'PublishingStatelessService' publishes messages of Type 'PublishedMessageOne'.

Add a Reliable Stateless Service project called 'PublishingStatelessService'.
Add Nuget package 'ServiceFabric.PubSubActors' to 'PublishingStatelessService'.
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces' to 'PublishingStatelessService'.
Add a project reference to the shared data contracts library ('DataContracts').

Go to the project 'DataContracts' and add an interface file IPublishingStatelessService.cs. 
Add the code below:
```javascript
[ServiceContract]
public interface IPublishingStatelessService : IService
{
	//allows external callers to trigger a publish action, not required for functionality
	[OperationContract]
	Task<string> PublishMessageOneAsync();
	[OperationContract]
	Task<string> PublishMessageTwoAsync();
}
```
Open the file 'PublishingStatelessService.cs'. Replace the contents with the code below:
```javascript
internal sealed class PublishingStatelessService : StatelessService, IPublishingStatelessService
{
	protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
	{
		yield return new ServiceInstanceListener(context => new FabricTransportServiceRemotingListener(context, this), "StatelessFabricTransportServiceRemotingListener");
	}

	async Task<string> IPublishingStatelessService.PublishMessageOneAsync()
	{
		ServiceEventSource.Current.ServiceMessage(this, "Publishing Message");
		await this.PublishMessageAsync(new PublishedMessageOne { Content = "Hello PubSub World, from Service!" });
		return "Message published tot broker actor";
	}
	
	async Task<string> IPublishingStatelessService.PublishMessageTwoAsync()
        {
            ServiceEventSource.Current.ServiceMessage(this, "Publishing Message");
            await this.PublishMessageToBrokerServiceAsync(new PublishedMessageTwo { Content = "Hello PubSub World, from Service, using Broker Service!" });
            return "Message published to broker service";
        }
}
```
