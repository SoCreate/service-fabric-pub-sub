# Service Fabric Pub/Sub (Not just for Actors anymore!)
Do you want to create an Event Driven Architecture while using Azure Service Fabric?  
Do you need to reliably broadcast messages between Actors and Services?  
This code will help you do that.  
It supports both Actors and Services as publishers and subscribers.

## Contribute!
Contributions are welcome.  
Please upgrade the package version with a minor tick if there are no breaking changes. And add a line to the readme.md, stating the changes, e.g. 'upgraded to SF version x.y.z'.  
Doing so will allow me to simply accept the PR, which will automatically trigger the release of a new package.  
Please also make sure all feature additions have a corresponding unit test.

## Release notes:

- 8.1.0 Add `GetBrokerStatsAsync()` and `UnsubscribeByQueueNameAsync()` to `BrokerClient` to help with monitoring and managing the Broker Service.
- 8.0.0 Major cleanup and usability improvements.  Replaced Helper classes with a single `BrokerClient`.  Removed obsolete code (BrokerActor, RelayBrokerActor, extension methods).  Removed the `ServiceFabric.PubSubActors.Interfaces` library.  Simplified the BrokerService interface.
- 7.6.2 Fix routing key issue.
- 7.6.1 Fix hashing helper null ref issue.
- 7.6.0 Add routing key support, to support attribute based messaging. Fix hashing issue in dotnet core.
- 7.5.0 Major upgrade. Added `SubscriberStatelessServiceBase`,`SubscriberStatefulServiceBase`, `StatefulSubscriberServiceBootstrapper` and `StatelessSubscriberServiceBootstrapper` classes to simplify managing subscriber services. Thanks @danadesrosiers.
- 7.4.3 Broker actor is now obsolete. The interfaces library will be removed as well.
- 7.4.2 BrokerServiceLocator located in other Application will now be found.
- 7.4.1 Upgraded nuget packages (SF 3.3.624).  Required updating BrokerServiceLocator to support V2 remoting.
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


## Getting started



Using this package you can reliably send messages from Publishers (Actors/Services) to many Subscribers (Actors/Services).  
This is done using an intermediate, which is the BrokerService.  
Add this package to all Reliable Actor & Service projects that participate in the pub/sub messaging.


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


## How to use:


Add a new Stateful Reliable Service project. Call it 'PubSubService' (optional).  
Add Nuget package 'ServiceFabric.PubSubActors' to the 'PubSubActor' project.  
Replace the code of PubSubService with the following code:
```csharp
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

*Optional, for 'Large Scale Messaging' extend `BrokerServiceUnordered` instead of `BrokerService`*

### Add a shared data contracts library
*Add a common datacontracts library for shared messages*

Add a new Class Library project, call it 'DataContracts', and add these sample message contracts:
```csharp
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

### Publishing messages from Service or Actor
```csharp
var brokerClient = new BrokerClient();
brokerClient.PublishMessageAsync(new PublishedMessageOne { Content = "Hello PubSub World, from Subscriber, using Broker Service!" })
```

### Subscribing to messages using Actors
*Create a sample Actor that implements 'ISubscriberActor', to become a subscriber to messages.*  
In this example, the Actor called 'SubscribingActor' subscribes to messages of Type 'PublishedMessageOne'.  

Add a Reliable Stateless Actor project called 'SubscribingActor'.  
Add Nuget package 'ServiceFabric.PubSubActors' to the 'SubscribingActor' project.  
Add a project reference to the shared data contracts library ('DataContracts').

Open the file 'SubscribingActor.cs' and replace the contents with the code below.  
**notice that this Actor implement 'ISubscriberActor'.**
```csharp
[ActorService(Name = nameof(SubscribingActor))]
[StatePersistence(StatePersistence.None)]
internal class SubscribingActor : Actor, ISubscriberActor
{
    private readonly IBrokerClient _brokerClient;

    public SubscribingActor(ActorService actorService, ActorId actorId, IBrokerClient brokerClient)
        : base(actorService, actorId)
    {
        _brokerClient = brokerClient ?? new BrokerClient();
    }

    protected override async Task OnActivateAsync()
    {
        await _brokerClient.SubscribeAsync<PublishedMessageOne>(this, HandleMessageOne);
    }

    public Task ReceiveMessageAsync(MessageWrapper message)
    {
        return _brokerClient.ProcessMessageAsync(message);
    }

    private Task HandleMessageOne(PublishedMessageOne message)
    {
        ActorEventSource.Current.ActorMessage(this, $"Received message: {message.Content}");
        return Task.CompletedTask;
    }
}
```

### Subscribing to messages using Services using our base class

*Create a sample Service that extends SubscriberStatelessServiceBase.*  
In this example, the Service called 'SubscribingStatelessService' subscribes to messages of Type 'PublishedMessageOne' and 'PublishedMessageTwo'.

Add a Reliable Stateless Service project called 'SubscribingStatelessService'.  
Add Nuget package 'ServiceFabric.PubSubActors'.  
Add a project reference to the shared data contracts library ('DataContracts').

Now open the file SubscribingStatelessService.cs in the project 'SubscribingStatelessService' and replace the SubscribingStatelessService class with this code:
```csharp
internal sealed class SubscribingStatelessService : SubscriberStatelessServiceBase
{
    public SubscribingStatelessService(StatelessServiceContext serviceContext, IBrokerClient brokerClient = null)
        : base(serviceContext, brokerClient)
    {
    }

    [Subscribe]
    private Task HandleMessageOne(PublishedMessageOne message)
    {
        ServiceEventSource.Current.ServiceMessage(Context, $"Processing PublishedMessageOne: {message.Content}");
        return Task.CompletedTask;
    }

    [Subscribe]
    private Task HandleMessageTwo(PublishedMessageTwo message)
    {
        ServiceEventSource.Current.ServiceMessage(Context, $"Processing PublishedMessageTwo: {message.Content}");
        return Task.CompletedTask;
    }
}
```
The SubscriberStatelessServiceBase class automatically handles subscribing to the message types that were registered using the `Subscribe` attribute when the service is 'opened'.  
*To subscribe from a Stateful Service, extend `SubscriberStatefulServiceBase` instead of `SubscribingStatelessServiceBase`*

### Subscribing to messages using Services without using our base class

If you don't want to inherit from our base classes, you can use the `StatefulSubscriberServiceBootstrapper` and `StatelessSubscriberServiceBootstrapper` as a wrapper around the service factory delegate in `Program.cs`.

The code in `Program` will look like this:

```csharp
    var brokerClient = new BrokerClient();
    ServiceRuntime.RegisterServiceAsync("SubscribingStatelessServiceType",
        context => new StatelessSubscriberServiceBootstrapper<SubscribingStatelessService>(context, ctx => new SubscribingStatelessService (ctx, brokerClient), brokerClient).Build())
        .GetAwaiter().GetResult();
```

The service looks like this:

```csharp
internal sealed class SubscribingStatelessService : StatelessService, ISubscriberService
{
    private readonly IBrokerClient _brokerClient;

    public SubscribingStatelessService(StatelessServiceContext serviceContext, IBrokerClient brokerClient = null) : base(serviceContext)
    {
        _brokerClient = brokerClient;
    }

    public Task ReceiveMessageAsync(MessageWrapper messageWrapper)
    {
        // Automatically delegates work to annotated methods withing this class.
        return _brokerClient.ProccessMessageAsync(messageWrapper);
    }

    protected override IEnumerable<ServiceInstanceListener> CreateServiceInstanceListeners()
    {
        return this.CreateServiceRemotingInstanceListeners();
    }

    [Subscribe]
    private Task HandleMessageOne(PublishedMessageOne message)
    {
        ServiceEventSource.Current.ServiceMessage(Context, $"Processing PublishedMessageOne: {message.Content}");
        return Task.CompletedTask;
    }

    [Subscribe]
    private Task HandleMessageTwo(PublishedMessageTwo message)
    {
        ServiceEventSource.Current.ServiceMessage(Context, $"Processing PublishedMessageTwo: {message.Content}");
        return Task.CompletedTask;
    }
}
```

Once the service gets an endpoint, it will automatically create subscriptions for all methods annotated with the `Subscribe` attribute.  
For stateful services, use the `StatefulSubscriberServiceBootstrapper`.

*Check the Demo project for a working reference implementation.*


## Routing
**This experimental feature works only when using the `DefaultPayloadSerializer`.**  
It adds support for an additional subscriber filter, based on message content.

### Example

Given this message type:

```csharp
public class CustomerMessage
{
    public Customer Customer {get; set;}
}

public class Customer
{
    public string Name {get; set;}
}
```
And given a subscriber that is interested in Customers named 'Customer1'.  
The subscription would be registered like this:

```csharp
await brokerClient.SubscribeAsync<CustomerMessage>(this, HandleMessageOne, routingKey: "Customer.Name=Customer1");
```

The routing key is queried by using `JToken.SelectToken`. More info [here](https://www.newtonsoft.com/json/help/html/SelectToken.htm).

## Monitoring the Broker
The `BrokerClient` offers a way to get some basic information about what is going on in the BrokerService.  Calling `BrokerClient.GetBrokerStats()` provides a Dictionary with one item for each queue (subscription) that the Broker is managing.  The key is the name of the queue and the value is a list of QueueStat objects.  Each QueueStat object is a snapshot of the state of the queue at the time the request was made.  The QueueStat object includes the name of the service, the total number of messages received, the total number of messages delivered, and time the snapshot was taken.  The totals are cumulative from the moment the queue was created.

A new snapshot is added each time `BrokerClient.GetBrokerStats()` is called, so the user has control over the polling frequency.  The `BrokerClient` keeps a fixed number of snapshots per queue defined by `BrokerClient.QueueStatCapacity`, which defaults to 100.  When the 101st snapshot is created, the oldest one will be trimmed from the list.

What information can you get:
* A list of all queues (subscriptions) and the subscriber services they serve.
* The total number of messages received and delivered for each queue.
* If the difference between the total received and the total delivered is growing, the queue is taking in messages faster than they can be delivered (or the subscriber is down).
* With a little extra processing, the number of messages received and delivered in a given time period can be calculated.  A UI could graph this data to show the load on each queue over time.

Armed with a list of queueNames, you can use `BrokerClient.UnsubscribeByQueueNameAsync()` to unsubscribe on behalf of another service.

## Upgrading to version 8
Significant changes were made in v8.0.0, including breaking changes to interfaces and tools.
* BrokerActor and RelayBrokerActor were removed.  Actors don't make a good Broker, use BrokerService instead.
* ServiceFabric.PubSubActors.Interfaces library was removed.  Only the main library is required now.
* Obsolete extension methods were removed.
* Helper classes were removed and replaced with `BrokerClient`.
* V2 is the only supported Service Fabric Remoting version.

Here are some tips to help you through the upgrade process:
1. Upgrade remoting to V2
    * v8.0 only supports remoting V2.  If you are using .NET Standard, you should already be using V2 and you can skip this step.
    * Refer to documentation here: [Upgrade From Remoting V1 to Remoting V2](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-services-communication-remoting#upgrade-from-remoting-v1-to-remoting-v2)
    * Upgrade the Broker first.  Override `BrokerService.CreateServiceReplicaListeners()` to use the extension method mentioned in the above link so you can use the assembly attribute to set up listening on V1 and V2.
    * Upgrade Publisher and Subscriber services.  Use the `useRemotingV2` option when creating the BrokerServiceLocator that is used by Helper classes.
2. Upgrade the Broker.
   * Remove the ServiceFabric.PubSubActors.Interfaces library and upgrade the ServiceFabric.PubSubActors library to 8.0.
   * Publish message and Receive message are backwards compatible, so update BrokerService first and publishers and subscribers should continue to function.
3. Upgrade Subscribers.
   * Register/Unregister has been replaced by Subscribe/Unsubscribe, so you won't be able to subscribe/unsubscribe until you update subscriber services.
   * Remove the ServiceFabric.PubSubActors.Interfaces library and upgrade the ServiceFabric.PubSubActors library to 8.0.
   * Extension methods and helper classes have been removed.  If you are using any of them in your Subscriber services, they will need to be refactored to use the BrokerClient or one of the Subscriber base classes (see documentation above).
4. Upgrade Publishers. (Optional, publishing is backwards compatible)
   * Remove the ServiceFabric.PubSubActors.Interfaces library and upgrade the ServiceFabric.PubSubActors library to 8.0.
   * Extension method and helper classes have been removed.  Use BrokerClient.PublishMessageAsync() instead.
