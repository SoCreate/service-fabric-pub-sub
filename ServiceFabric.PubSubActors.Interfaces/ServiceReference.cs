﻿using System;
using System.Fabric;
using System.Runtime.Serialization;

namespace ServiceFabric.PubSubActors.Interfaces
{
	[DataContract]
	public class ServiceReference
	{
		/// <summary>
		/// Gets or sets Service ApplicationName of the Service in service fabric cluster.
		/// </summary>
		[DataMember(IsRequired = true)]
		public string ApplicationName { get; set; }

		/// <summary>
		/// Gets or sets Service uri which hosts the Service in service fabric cluster.
		/// </summary>
		[DataMember(IsRequired = true)]
		public Uri ServiceUri { get; set; }

        [DataMember(IsRequired = false)]
	    public long PartitionKey { get; set; }

		[DataMember(IsRequired = true)]
		public ServicePartitionKind PartitionKind { get; set; }

		[DataMember(IsRequired = false)]
		public string PartitionName { get; set; }

		[DataMember(IsRequired = false)]
		public long? PartitionID { get; set; }

        [DataMember(IsRequired = false)]
		public Guid PartitionGuid { get; set; }

	    [DataMember(IsRequired = false)]
	    public string ListenerName { get; set; }

        public string Description
		{

			get
			{
				string description;

				switch (PartitionKind)
				{
					case ServicePartitionKind.Invalid:
						description = $"{ServiceUri} - ID:invalid - Guid:{PartitionGuid}";
						break;
					case ServicePartitionKind.Singleton:
						description = $"{ServiceUri} - ID:singleton - Guid:{PartitionGuid}";
						break;
					case ServicePartitionKind.Int64Range:
						description = $"{ServiceUri} - ID:{PartitionID} - Guid:{PartitionGuid}";
						break;
					case ServicePartitionKind.Named:
						description = $"{ServiceUri} - ID:{PartitionName} - Guid:{PartitionGuid}";
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
				return description;
			}
		}
	}
}
