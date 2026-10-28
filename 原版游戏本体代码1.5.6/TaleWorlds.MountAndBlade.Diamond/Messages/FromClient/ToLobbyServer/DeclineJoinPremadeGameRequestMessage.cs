using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class DeclineJoinPremadeGameRequestMessage : Message
{
	[JsonProperty]
	public Guid PartyId { get; private set; }

	public DeclineJoinPremadeGameRequestMessage()
	{
	}

	public DeclineJoinPremadeGameRequestMessage(Guid partyId)
	{
		PartyId = partyId;
	}
}
