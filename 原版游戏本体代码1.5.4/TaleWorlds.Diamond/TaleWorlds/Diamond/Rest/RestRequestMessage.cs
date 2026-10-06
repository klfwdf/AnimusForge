using System;
using System.Runtime.Serialization;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public abstract class RestRequestMessage : RestData
{
	[DataMember]
	public byte[] UserCertificate { get; set; }
}
