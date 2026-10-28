using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", false)]
public class NewPlayerResponseMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public PlayerBattleServerInformation PlayerBattleInformation { get; private set; }

	public NewPlayerResponseMessage()
	{
	}

	public NewPlayerResponseMessage(PlayerId playerId, PlayerBattleServerInformation playerBattleInformation)
	{
		PlayerId = playerId;
		PlayerBattleInformation = playerBattleInformation;
	}
}
