# Service Fabric Pub/Sub Actors
Do you want to create an Event Driven Architecture while using Azure Service Fabric?
Do you need to reliably broadcast messages between Actors and Services?
This code will help you do that.
It supports both Actors and Services as publishers and subscribers.

## Nuget:
https://www.nuget.org/packages/ServiceFabric.PubSubActors/2.0.0-preview
https://www.nuget.org/packages/ServiceFabric.PubSubActors.Interfaces/2.0.0-preview

## Introduction
Using this package you can reliably send messages from Publishers (Actors/Services) to many Subscribers (Actors/Services). 
This is done using an intermediate, called BrokerActor.
Add this package to all Reliable Actor projects that participate in the pub/sub messaging.
Add the package 'ServiceFabric.PubSubActors.Interfaces' to all (*ReliableActor).Interfaces projects.

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

## How to use:

### Create a BrokerActor type
*Actors of this type will be used to register subscribers to, and every instance will publish one type of message.*

Add this package to a new Stateful Reliable Actor project. Call it 'PubSubActor'.
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

### Add a shared data contracts library
*Add a common datacontracts library for shared messages*

Add a new Class Library project, call it 'DataContracts', and add this sample message contract:
```javascript
[DataContract]
public class PublishedMessageOne
{
	[DataMember]
	public string Content { get; set; }
}
```

### Subscribing to messages using Actors
*Create a sample Actor that implements 'ISubscriberActor', to become a subscriber to messages.*
In this example, the Actor called 'SubscribingActor' subscribes to messages of Type 'PublishedMessageOne'.

Add a Reliable Stateless Actor project called 'SubscribingActor'.
Add Nuget package 'ServiceFabric.PubSubActors'.
Add a reference to the shared data contracts library ('DataContracts').
Go to the SubscribingActor.Interfaces project, open the file 'ISubscribingActor' and replace the contents with this code:
**notice this implements ISubscriberActor from the package 'ServiceFabric.PubSubActors.Interfaces'**
```javascript
public interface ISubscribingActor : ISubscriberActor  
{		
}
```

Open the file 'SubscribingActor.cs' and replace the contents with the code below.
**notice that this Actor now inherits from 'StatelessSubscriberActor'.**
```javascript
[ActorService(Name = nameof(ISubscribingActor))] //required because of interface operations
internal class SubscribingActor : StatelessSubscriberActor, ISubscribingActor
{
	public Task RegisterAsync()
	{
		return RegisterMessageTypeAsync(typeof(PublishedMessageOne));  //register as subscriber for this type of messages
	}

	public Task ReceiveMessageAsync(MessageWrapper message)
	{
		var payload = Deserialize<PublishedMessageOne>(message);
		ActorEventSource.Current.ActorMessage(this, $"Received message: {payload.Content}");
		//TODO: handle message
		return Task.FromResult(true);
	}
}
```
You can now register 'SubscriberActor' using 'RegisterAsync' to receive messages from the PubSubActor using 'ReceiveMessageAsync'. 
Do so by adding this code to the Program.Main method.
__Add this code after this line: 'fabricRuntime.RegisterActor<SubscribingActor>();' and before 'Thread.Sleep(Timeout.Infinite);'__

```javascript
ActorId actorId = new ActorId("SubActor");
string applicationName = "fabric:/MyServiceFabricApp"; //replace with your application name
ISubscriberActor subActor = ActorProxy.Create<ISubscribingActor>(actorId, applicationName, nameof(ISubscribingActor));
subActor.RegisterAsync().GetAwaiter().GetResult();
```

### Subscribing to messages using Services
*Create a sample Service that implements 'ISubscriberService', to become a subscriber to messages.*
In this example, the Service called 'SubscribingStatefulService' subscribes to messages of Type 'PublishedMessageOne'.

Add a Reliable Stateless Service project called 'SubscribingStatefulService'.
Add Nuget package 'ServiceFabric.PubSubActors'.
Add a reference to the shared data contracts library ('DataContracts').
Implement 'ServiceFabric.PubSubActors.SubscriberServices.ISubscriberService'.

Now open the file SubscribingStatefulService.cs in the project 'SubscribingStatefulService' and replace the contents with this code:

```javascript
using ServiceFabric.PubSubActors.PublisherActors;
internal sealed class SubscribingStatefulService : StatefulService, ISubscriberService
{
	protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
	{
		//add SubscriberCommunicationListener for receiving published messages.
		yield return new ServiceReplicaListener(p => new SubscriberCommunicationListener(this, p));
	}

	public Task RegisterAsync()
	{
		return this.RegisterMessageTypeAsync(typeof(PublishedMessageOne));
	}

	public Task UnregisterAsync()
	{
		return this.UnregisterMessageTypeAsync(typeof(PublishedMessageOne), true);
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
Call 'RegisterAsync' to make the service register itself as subscriber.
You can do that from the outside, or within the service itself, provided 'CreateServiceReplicaListeners' has been called first.

```javascript
var pubActor = ServiceProxy.Create<IPublishingStatelessService>(serviceName);
pubActor.RegisterAsync().GetAwaiter().GetResult();
```


### Publishing messages from Actors
*Create a sample Actor that publishes messages.*
In this example, the Actor called 'PublishingActor' publishes messages of Type 'PublishedMessageOne'.

In this example, the Publisher Actor publishes messages of Type 'PublishedMessageOne'.
Add a Reliable Stateless Actor project called 'PublishingActor'.
Add Nuget package 'ServiceFabric.PubSubActors'.
Add a reference to the shared data contracts library ('DataContracts').
Go to the project 'PublishingActor.Interfaces' and open the file IPublishingActor.cs. 
Replace the contents with the code below:

```javascript
public interface IPublishingActor : IActor
{
	Task<string> PublishMessageOneAsync();
}
```

Now open the file PublishingActor.cs in the project 'PublishingActor' and replace the contents with this code:

```javascript
using ServiceFabric.PubSubActors.PublisherActors;
//no attribute required.
internal class PublishingActor : StatelessActor, IPublishingActor
{
	async Task<string> IPublishingActor.PublishMessageOneAsync()
	{
		ActorEventSource.Current.ActorMessage(this, "Publishing Message");
		await this.PublishMessageAsync(new PublishedMessageOne {Content = "Hello PubSub World, from Actor!"});
		return "Message published";
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
Add Nuget package 'ServiceFabric.PubSubActors'.
Add a reference to the shared data contracts library ('DataContracts').
Go to the project 'DataContracts' and add an interface file IPublishingStatelessService.cs. 
Add the code below:
```javascript
[ServiceContract]
public interface IPublishingStatelessService : IService
{
	[OperationContract]
	Task<string> PublishMessageOneAsync();
}
```
Open the file 'PublishingStatelessService.cs'. Replace the contents with the code below:
```javascript
internal sealed class PublishingStatelessService : StatelessService, IPublishingStatelessService
{
	protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
	{
		yield return new ServiceInstanceListener(parameters => new ServiceRemotingListener<IPublishingStatelessService>(parameters, this));
	}

	async Task<string> IPublishingStatelessService.PublishMessageOneAsync()
	{
		ServiceEventSource.Current.ServiceMessage(this, "Publishing Message");
		await this.PublishMessageAsync(new PublishedMessageOne { Content = "Hello PubSub World, from Service!" });
		return "Message published";
	}
}
```
