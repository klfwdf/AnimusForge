using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class AssignAsClanOfficerMessage : Message
{
	[JsonProperty]
	public PlayerId AssignedPlayerId { get; private set; }

	[JsonProperty]
	public bool DontUseNameForUnknownPlayer { get; private set; }

	public AssignAsClanOfficerMessage()
	{
	}

	public AssignAsClanOfficerMessage(PlayerId assignedPlayerId, bool dontUseNameForUnknownPlayer)
	{
		AssignedPlayerId = assignedPlayerId;
		DontUseNameForUnknownPlayer = dontUseNameForUnknownPlayer;
	}
}
