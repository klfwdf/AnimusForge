using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
public class NewPlayerMessage : Message
{
	[JsonProperty]
	public PlayerBattleInfo PlayerBattleInfo { get; private set; }

	[JsonProperty]
	public PlayerData PlayerData { get; private set; }

	[JsonProperty]
	public Guid PlayerParty { get; private set; }

	[JsonProperty]
	public Dictionary<string, List<string>> UsedCosmetics { get; private set; }

	public NewPlayerMessage()
	{
	}

	public NewPlayerMessage(PlayerData playerData, PlayerBattleInfo playerBattleInfo, Guid playerParty, Dictionary<string, List<string>> usedCosmetics)
	{
		PlayerBattleInfo = playerBattleInfo;
		PlayerData = playerData;
		PlayerParty = playerParty;
		UsedCosmetics = usedCosmetics;
	}
}
