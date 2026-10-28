using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", false)]
public class PlayerDisconnectedMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public DisconnectType Type { get; private set; }

	[JsonProperty]
	public bool IsAllowedLeave { get; private set; }

	[JsonProperty]
	public BattleResult BattleResult { get; private set; }

	public PlayerDisconnectedMessage()
	{
	}

	public PlayerDisconnectedMessage(PlayerId playerId, DisconnectType type, bool isAllowedLeave, BattleResult battleResult)
	{
		PlayerId = playerId;
		Type = type;
		IsAllowedLeave = isAllowedLeave;
		BattleResult = battleResult;
	}
}
