using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromBattleServerManager.ToBattleServer;

[Serializable]
[MessageDescription("BattleServerManager", "BattleServer", true)]
public class BattleReadyResponseMessage : FunctionResult
{
	[JsonProperty]
	public bool ShouldReportActivities { get; private set; }

	public BattleReadyResponseMessage()
	{
	}

	public BattleReadyResponseMessage(bool shouldReportActivities)
	{
		ShouldReportActivities = shouldReportActivities;
	}
}
