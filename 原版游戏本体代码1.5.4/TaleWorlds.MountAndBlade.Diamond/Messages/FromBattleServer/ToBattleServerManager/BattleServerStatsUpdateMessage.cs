using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", false)]
public class BattleServerStatsUpdateMessage : Message
{
	[JsonProperty]
	public BattleResult BattleResult { get; private set; }

	[JsonProperty]
	public Dictionary<int, int> TeamScores { get; private set; }

	public BattleServerStatsUpdateMessage()
	{
	}

	public BattleServerStatsUpdateMessage(BattleResult battleResult, Dictionary<int, int> teamScores)
	{
		BattleResult = battleResult;
		TeamScores = teamScores;
	}
}
