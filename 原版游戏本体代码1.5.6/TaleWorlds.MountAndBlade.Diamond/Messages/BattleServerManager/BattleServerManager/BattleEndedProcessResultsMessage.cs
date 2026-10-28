using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.BattleServerManager.BattleServerManager;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServerManager", true)]
public class BattleEndedProcessResultsMessage : Message
{
	[JsonProperty]
	public BattleResult BattleResult { get; private set; }

	[JsonProperty]
	public List<BadgeDataEntry> BadgeDateEntries { get; private set; }

	[JsonProperty]
	public string BattleGameType { get; private set; }

	[JsonProperty]
	public string Region { get; private set; }

	[JsonProperty]
	public List<(PlayerBattleInfo playerBattleInfo, bool shouldSendMessage, bool hasLeftGame)> PlayersForResults { get; private set; }

	[JsonProperty]
	public Guid BattleId { get; private set; }

	public BattleEndedProcessResultsMessage()
	{
	}

	public BattleEndedProcessResultsMessage(BattleResult battleResult, List<BadgeDataEntry> badgeDateEntries, string battleGameType, string region, List<(PlayerBattleInfo playerBattleInfo, bool shouldSendMessage, bool hasLeftGame)> playersForResults)
		: this(battleResult, badgeDateEntries, battleGameType, region, playersForResults, Guid.Empty)
	{
	}

	public BattleEndedProcessResultsMessage(BattleResult battleResult, List<BadgeDataEntry> badgeDateEntries, string battleGameType, string region, List<(PlayerBattleInfo playerBattleInfo, bool shouldSendMessage, bool hasLeftGame)> playersForResults, Guid battleId)
	{
		BattleResult = battleResult;
		BadgeDateEntries = badgeDateEntries;
		BattleGameType = battleGameType;
		Region = region;
		PlayersForResults = playersForResults;
		BattleId = battleId;
	}
}
