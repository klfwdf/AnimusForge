using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond.Rest;

[Serializable]
[DataContract]
public class AliveMessage : RestRequestMessage
{
	[DataMember]
	public SessionCredentials SessionCredentials { get; private set; }

	public AliveMessage()
	{
	}

	[JsonConstructor]
	public AliveMessage(SessionCredentials sessionCredentials)
	{
		SessionCredentials = sessionCredentials;
	}
}
