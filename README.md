# Service Fabric Pub/Sub (Not just for Actors anymore!)
Do you want to create an Event Driven Architecture while using Azure Service Fabric?
Do you need to reliably broadcast messages between Actors and Services?
This code will help you do that.
It supports both Actors and Services as publishers and subscribers.

It uses extension methods to
- Actor
- StatelessService
- StatefulService

## Contribute!
Contributions are welcome.
Please upgrade the package version with a minor tick if there are no breaking changes. And add a line to the readme.md, stating the changes, e.g. 'upgraded to SF version x.y.z'.
Doing so will allow me to simply accept the PR, which will automatically trigger the release of a new package.
Please also make sure all feature additions have a corresponding unit test.

## Release notes:

- 7.4.0 Add NETSTANDARD2.0 version to the package. Allow SF Remoting V1/V2 for full framework. Requested by alexmarshall132 and danijel-peric in issue 45.
- 7.3.7 Fix implementation of `ServiceReferenceWrapper.Equals` to allow changing partitionid's. As reported by danijel-peric in issue 44.
- 7.3.6 Fix call to `GetPartitionForMessageAsync` with wrong argument. As reported by danijel-peric in issue 43.
- 7.3.5 Upgraded nuget packages (SF 3.1.283). Fixed null ref issue in `ServiceReferenceWrapper` after restarting broker. As reported by danijel-peric in issue 41.
- 7.3.4 Added support for named listeners
- 7.3.3 Upgraded nuget packages (SF 3.0.472)
- 7.3.2 Upgraded nuget packages (SF 3.0.456)
- 7.3.1 Sign assemblies in packages
- 7.3.0 Upgraded nuget packages (SF 2.8.232) & removed WCF remoting code
- 7.2.0 Upgraded nuget packages (SF 2.8.219)
- 7.1.1 Upgraded nuget packages (SF 2.7.198)
- 7.1.0 upgraded to new sdk (2.6.220)
- 7.0.0 upgraded to new sdk (2.6.210) and VS2017
- 6.0.3 Add experimental support for IReliableConcurrentQueue, using `BrokerServiceUnordered`. Used in the LoadDemo app.
- 5.1.0 Add custom serialization option for kotvisbj
- 5.0.0 Upgraded nuget packages (SF 2.5.216)
- 4.9.1 merged PR by johnkattenhorn that changes 2 consts into properties on BrokerService
- 4.9.0 upgraded to new SDK (2.4.164) 
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
	``` csharp
	using ServiceFabric.PubSubActors.PublisherActors;
	using ServiceFabric.PubSubActors.Helpers;
	[..]
	var publisherActorHelper = new PublisherActorHelper(new BrokerServiceLocator());
	await publisherActorHelper.PublishMessageAsync(this, new PublishedMessageTwo { Content = "Hello PubSub World, from Actor, using Broker Service!" });
        //use any JSON serializable object as a message, not just PublishedMessageTwo
	```

	3.2. From a Service:  
	``` csharp
	using ServiceFabric.PubSubActors.PublisherServices;
	using ServiceFabric.PubSubActors.Helpers;
	[..]
	var publisherServiceHelper = new PublisherServiceHelper(new BrokerServiceLocator());
	await publisherServiceHelper.PublishMessageAsync(this, new PublishedMessageTwo { Content = "Hello PubSub World, from Service, using Broker Service!" });
	//use any JSON serializable object as a message
	```

	*you can use dependency injection with the Publisher helpers to facilitate unit testing*
	
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

**Preferably, just use the BrokerService/BrokerServiceUnordered**

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
https://github.com/loekd/ServiceFabric.PubSubActors/blob/master/ServiceFabric.PubSubActors.Demo/SubscribingActor/SubscribingActor.cs


### Subscribing to messages using Services
*Create a sample Service that implements 'ISubscriberService', to become a subscriber to messages.*
In this example, the Service called 'SubscribingStatefulService' subscribes to messages of Type 'PublishedMessageOne'.

Add a Reliable Stateful Service project called 'SubscribingStatefulService'.
Add Nuget package 'ServiceFabric.PubSubActors'.
Add Nuget package 'ServiceFabric.PubSubActors.Interfaces'.
Add a project reference to the shared data contracts library ('DataContracts').

Now open the file SubscribingStatefulService.cs in the project 'SubscribingStatefulService' and replace the contents with this code:
(Implement 'ServiceFabric.PubSubActors.SubscriberServices.ISubscriberService' and self-register.)

https://github.com/loekd/ServiceFabric.PubSubActors/blob/master/ServiceFabric.PubSubActors.Demo/SubscribingStatefulService/SubscribingStatefulService.cs

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

https://github.com/loekd/ServiceFabric.PubSubActors/blob/master/ServiceFabric.PubSubActors.Demo/PublishingActor/PublishingActor.cs

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

https://github.com/loekd/ServiceFabric.PubSubActors/blob/master/ServiceFabric.PubSubActors.Demo/PublishingStatelessService/PublishingStatelessService.cs
