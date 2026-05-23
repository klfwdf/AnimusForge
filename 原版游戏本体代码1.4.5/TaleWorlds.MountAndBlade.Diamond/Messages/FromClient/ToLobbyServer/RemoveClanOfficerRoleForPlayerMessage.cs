using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromClient.ToLobbyServer;

[Serializable]
[MessageDescription("Client", "LobbyServer", true)]
public class RemoveClanOfficerRoleForPlayerMessage : Message
{
	[JsonProperty]
	public PlayerId RemovedOfficerId { get; private set; }

	public RemoveClanOfficerRoleForPlayerMessage()
	{
	}

	public RemoveClanOfficerRoleForPlayerMessage(PlayerId removedOfficerId)
	{
		RemovedOfficerId = removedOfficerId;
	}
}
