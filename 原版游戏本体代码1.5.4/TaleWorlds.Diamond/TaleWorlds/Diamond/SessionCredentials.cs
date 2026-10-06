using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[DataContract]
public sealed class SessionCredentials
{
	[DataMember]
	public PeerId PeerId { get; private set; }

	[DataMember]
	public SessionKey SessionKey { get; private set; }

	[JsonConstructor]
	public SessionCredentials(PeerId peerId, SessionKey sessionKey)
	{
		PeerId = peerId;
		SessionKey = sessionKey;
	}
}
