using System.Fabric;

namespace ServiceFabric.PubSubActors.State
{
	public static class PartitionInfoExtensions
	{
		public static Int64RangePartitionInformation AsLongPartitionInfo(this ServicePartitionInformation info)
		{
			return info as Int64RangePartitionInformation;
		}

		public static NamedPartitionInformation AsNamedPartitionInfo(this ServicePartitionInformation info)
		{
			return info as NamedPartitionInformation;
		}
	}
}