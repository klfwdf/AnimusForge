using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
public class PlayerDisconnectedFromLobbyMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public PlayerDisconnectedFromLobbyMessage()
	{
	}

	public PlayerDisconnectedFromLobbyMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
