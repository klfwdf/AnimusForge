using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
public class FriendlyDamageKickPlayerResponseMessage : Message
{
	[JsonProperty]
	public PlayerId PlayerId { get; private set; }

	public FriendlyDamageKickPlayerResponseMessage()
	{
	}

	public FriendlyDamageKickPlayerResponseMessage(PlayerId playerId)
	{
		PlayerId = playerId;
	}
}
