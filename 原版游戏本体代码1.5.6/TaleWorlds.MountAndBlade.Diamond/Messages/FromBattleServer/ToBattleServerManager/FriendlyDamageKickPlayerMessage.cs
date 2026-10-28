using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", false)]
public class FriendlyDamageKickPlayerMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	[JsonProperty]
	public Dictionary<int, (int killCount, float damage)> RoundDamageMap { get; private set; }

	public FriendlyDamageKickPlayerMessage()
	{
	}

	public FriendlyDamageKickPlayerMessage(PlayerId playerId, Dictionary<int, (int killCount, float damage)> roundDamageMap)
	{
		PlayerId = playerId;
		RoundDamageMap = roundDamageMap;
	}
}
