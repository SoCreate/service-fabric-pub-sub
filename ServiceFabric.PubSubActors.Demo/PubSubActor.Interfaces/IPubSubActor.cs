namespace PubSubActor.Interfaces
{
	/// <summary>
	/// This interface represents the actions a client app can perform on an actor.
	/// It MUST derive from IActor and all methods MUST return a Task.
	/// </summary>
	public interface IPubSubActor : ServiceFabric.PubSubActors.Interfaces.IBrokerActor
	{
	
	}
}
