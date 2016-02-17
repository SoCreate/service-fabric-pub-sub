using System.Runtime.Serialization;

namespace Common.DataContracts
{
	[DataContract]
	public class PublishedMessageOne
	{
		[DataMember]
		public string Content { get; set; }
	}
}
