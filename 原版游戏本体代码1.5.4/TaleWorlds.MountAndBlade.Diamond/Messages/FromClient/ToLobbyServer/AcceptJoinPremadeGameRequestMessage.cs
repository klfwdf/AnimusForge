using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class AcceptJoinPremadeGameRequestMessage : Message
{
	[JsonProperty]
	public Guid PartyId { get; private set; }

	public AcceptJoinPremadeGameRequestMessage()
	{
	}

	public AcceptJoinPremadeGameRequestMessage(Guid partyId)
	{
		PartyId = partyId;
	}
}
