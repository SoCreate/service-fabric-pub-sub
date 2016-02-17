# Service Fabric Pub/Sub Actors

##Introduction
Using this package you can reliably send messages from PublisherActor to many SubscriberActors. 
This is done using an intermediate, called BrokerActor.
Add this package to all Reliable Actor projects that participate in the pub/sub messaging.
Add the package 'ServiceFabric.PubSubActors.Interfaces' to all (*ReliableActor).Interfaces projects.

|    publisher  |     broker    |subscriber|
| ------------- |:-------------:| -----:|
|[PublisherActor]||
||[BrokerActor]|
|||[SubscriberActor]|
|||[SubscriberActor]|

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

### Subscribing to messages
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


### Publishing messages
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
**notice that this Actor now inherits from 'StatelessPublisherActor'.**
```javascript
//no attribute required.
internal class PublishingActor : StatelessPublisherActor, IPublishingActor
{
	async Task<string> IPublishingActor.PublishMessageOneAsync()
	{
		ActorEventSource.Current.ActorMessage(this, "Publishing Message");
		await PublishMessageAsync(new PublishedMessageOne {Content = "Hello PubSub World!"});
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


