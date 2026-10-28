using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
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
