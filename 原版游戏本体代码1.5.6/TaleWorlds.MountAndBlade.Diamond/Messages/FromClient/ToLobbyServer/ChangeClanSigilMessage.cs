using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class ChangeClanSigilMessage : Message
{
	[JsonProperty]
	public string NewSigil { get; private set; }

	public ChangeClanSigilMessage()
	{
	}

	public ChangeClanSigilMessage(string newSigil)
	{
		NewSigil = newSigil;
	}
}
