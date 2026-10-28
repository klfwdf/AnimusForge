using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace TaleWorlds.Diamond;

[Serializable]
[DataContract]
public abstract class LoginMessage : Message
{
	[DataMember]
	public PeerId PeerId { get; set; }

	[JsonProperty]
	public AccessObject AccessObject { get; private set; }

	public LoginMessage()
	{
	}

	protected LoginMessage(PeerId peerId, AccessObject accessObject)
	{
		PeerId = peerId;
		AccessObject = accessObject;
	}
}
