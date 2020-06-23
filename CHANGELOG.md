# 12.2.0 (2020-06-23)
Big thanks to @Calidus for contributing this feature and bug fix!
### Features
* **RoutingKey**: RoutingKey value is now a Regex to provide more flexibility.
### Bug Fix
* **RoutingKey**: ReferenceWrapper was updated to properly serialize the RoutingKey.

# 12.1.0 (2020-06-03)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 4.1.409).

# 12.0.1 (2020-01-27)
### Bug Fix
* **SubscribeAsync**: Fixed bug getting the message type name from generic type.  Thanks to @tbuquet for reporting and fixing this bug!

# 12.0.0 (2020-01-02)
### Breaking Changes
* **Nuget**: Upgraded nuget packages (SF 4.0.457).

# 11.0.0 (2019-07-01)
### Features
* **BrokerService**: BrokerService now supports both ordered and unordered queues.
* **SubscribeAttribute**: Adding options to configure queue type (ordered or unordered) and routingKey.
### Breaking Changes
* **BrokerService**: BrokerServiceUnordered was removed.  BrokerServiceBase was removed.

# 10.0.0 (2019-05-30)
### Features
* **Broker Events**: Added MessagePublished event.  Thanks to @BoeseB for suggesting this feature!
### Breaking Changes
* **Broker Events**: Renamed MessageReceived event to MessageQueuedToSubscriber.

# 9.1.1 (2019-05-28)
### Bug fix
* **Broker Stats**: Use concurrent dictionary to allow concurrent write access to stats.

# 9.1.0 (2019-04-01)
### Features
* **Subscribe Retry**: Added a retry strategy for when a subscriber fails to subscribe because the Broker doesn't exist yet.
* **BrokerServiceUri**: We recently lost the ability to specify the BrokerUri when publishing/subscribing.  Added the ability to pass the BrokerUri to the BrokerServiceLocator class.
* **Throttle on Failure**: Added a config to slow down the processing loop when errors occur.
* **Filter Broker Stats**: Added ability to filter on time, Service name, or message type using query parameters in the GET broker/stats API in the demo app.

# 9.0.0 (2019-03-21)
### Features
* **Broker Stats**: Added `GetBrokerStatsAsync()` and `UnsubscribeByQueueNameAsync()` to `BrokerClient` to help with monitoring and managing the Broker Service.
* **BrokerEvents**: Added BrokerEventsManager allowing users to add custom callbacks on Broker events to implement custom logging and/or monitoring functionality.
### Cleanup
* **Demo App**: Updated Demo app to .NET Core 2.2.  Integrated LoadDemo into the Demo app.
* **Organization**: Reorganized the repository to group projects into src, test, and examples directories.
### Breaking Changes
* **Rename**: Changed the project name to ServiceFabric.PubSub.  Updated namespaces accordingly.  The new Nuget package is: https://www.nuget.org/packages/SoCreate.ServiceFabric.PubSub.
### Dependencies
* **Nuget**: Update Nuget packages (SF 3.3.644)

# 8.0.0 (2019-03-05)
### Features
* **BrokerClient**: Replaced Helper classes with a single `BrokerClient` that handles all interaction with the Broker.
### Breaking Changes
* **Removed obsolete code**: BrokerActor, RelayBrokerActor, extension methods, Helper classes.  Removed the `ServiceFabric.PubSubActors.Interfaces` library.
* **IBrokerService**: Simplified `IBrokerService` interface to have `Subscribe()` and `Unsubscribe()` taking a generic `ReferenceWrapper` instead of multiple versions of `Register` and `Unregister` for different subscriber types.

# 7.6.2 (2019-03-04)
### Bug Fixes
* **Routing Key**: Fixed routing key issue.

# 7.6.1 (2019-03-04)
### Bug Fixes
* **RoutingKey**: Fixed hashing helper null ref issue.

# 7.6.0 (2019-03-02)
### Features
* **Routing Key**: Added routing key support, to support attribute based messaging. Fix hashing issue in dotnet core.

# 7.5.0 (2019-02-21)
### Features
* **Subscriber Base Classes**: Added `SubscriberStatelessServiceBase`,`SubscriberStatefulServiceBase`, `StatefulSubscriberServiceBootstrapper` and `StatelessSubscriberServiceBootstrapper` classes to simplify managing subscriber services. Thanks @danadesrosiers.

# 7.4.3 (2019-02-18)
### Deprecation
* **BrokerActor**: Broker actor is now obsolete and will be removed in a future release.
* **PubSubActors.Interfaces**: The interfaces library will be removed as well.

# 7.4.2 (2019-02-04)
### Features
* **BrokerServiceLocator**: Can now locate the Broker when it is in other Application.

# 7.4.1 (2019-01-28)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 3.3.624).  Required updating BrokerServiceLocator to support V2 remoting.

# 7.4.0 (2018-07-04)
### Features
* **NETSTANDARD2.0**: Added .NET Standard 2.0 version to the package.
* **SF Remoting**: Allow SF Remoting V1/V2 for full framework. Requested by alexmarshall132 and danijel-peric in issue #45.

# 7.3.7 (2018-06-26)
### Bug Fixes
* **PartitionIds**: Fix implementation of `ServiceReferenceWrapper.Equals` to allow changing partitionid's. As reported by danijel-peric in issue #44.

# 7.3.6 (2018-06-20)
### Bug Fixes
* **GetPartitionForMessageAsync**: Fix call to `GetPartitionForMessageAsync` with wrong argument. As reported by danijel-peric in issue #43.

# 7.3.5 (2018-06-14)
### Bug Fixes
* **ServiceReferenceWrapper**: Fixed null ref issue in `ServiceReferenceWrapper` after restarting broker. As reported by danijel-peric in issue #41.
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 3.1.283).

# 7.3.4 (2018-05-08)
### Features
* **Named Listeners**: Added support for named listeners

# 7.3.3 (2018-03-09)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 3.0.472).

# 7.3.2 (2018-02-07)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 3.0.456).

# 7.3.1 (2017-12-15)
### Features
* **Sign Assemblies**: Sign assemblies in packages.

# 7.3.0 (2017-12-03)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 3.0.232)
### Cleanup
* **WCF***: Removed WCF remoting code.

# 7.2.0 (2017-10-25)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 2.8.219).

# 7.1.1 (2017-08-18)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 2.7.198).

# 7.1.0 (2017-06-28)
### Dependencies
* **Nuget**: upgraded to new sdk (2.6.220).

# 7.0.0 (2017-05-31)
### Dependencies
* **Nuget**: upgraded to new sdk (2.6.210) and VS2017.

# 6.0.3 (2017-04-20)
### Features
* **BrokerServiceUnordered**: Add experimental support for IReliableConcurrentQueue, using `BrokerServiceUnordered`. Used in the LoadDemo app.

# 5.1.0 (2017-03-24)
### Features
* **Serialization**: Add custom serialization option for kotvisbj.

# 5.0.0 (2017-03-24)
### Dependencies
* **Nuget**: Upgraded nuget packages (SF 2.5.216).

# 4.9.1 (2017-02-21)
### Features
* **BrokerService**: Merged PR by johnkattenhorn that changes 2 consts into properties on BrokerService.

# 4.9.0 (2017-06-06)
### Dependencies
* **Nuget**: Upgraded to new SDK (2.4.164).

# 4.8.1 (2017-01-19)
### Features
* **BrokerService**: BrokerService.Subscribers is now protected, not private.

# 4.8.0 (2016-12-20)
### Dependencies
* **Nuget**: Upgraded to new SDK (2.4.145).

# 4.7.1 (2016-12-05)
### Bug Fixes
* **Dispose Issue**: Merged pull request by Sterlingg that fixes Dispose issue.

# 4.7.0 (2016-12-20)
### Dependencies
* **Nuget**: Upgraded to new SDK (2.3.311).

# 4.6.1 (2016-11-20)
### Bug Fix
* **ReferenceWrapper Equals**: Merged pr #2 by kelvintmv.  Using overridded Equals instead of == operator for `ReferenceWrapper` check.

# 4.6.0 (2016-10-17)
### Dependencies
* **Nuget**: Upgraded to new SDK and packages (2.3.301).

# 4.5.4 (2016-09-27)
### Bug Fixes
* **Unregister Issue**: Fixed unregister issue found by schernets.

# 4.5.0 (2016-09-13)
### Features
* **Helpers**: Moving from extension methods to injectable helpers for test support.
### Dependencies
* **Nuget**: Updated nuget packages (new SDK).

# 4.4.13 (2016-08-26)
### Bug Fixes
* **Memory Leak**: Fixed memory leak.

# 4.4.0 (2016-08-24)
### Features
* **BrokerService**: Improved BrokerService throughput.
* **Load Test Demo**: Added load test demo app.

# 4.2.0 (2016-08-05)
### Features
* **BrokerService**: Added BrokerService as counterpart of BrokerActor, so you can use your favorite programming model.
