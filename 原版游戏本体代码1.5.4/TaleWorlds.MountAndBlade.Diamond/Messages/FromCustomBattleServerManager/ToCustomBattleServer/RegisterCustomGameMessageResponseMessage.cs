using System;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromCustomBattleServerManager.ToCustomBattleServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServer", true)]
public class RegisterCustomGameMessageResponseMessage : FunctionResult
{
	[JsonProperty]
	public bool ShouldReportActivities { get; private set; }

	public RegisterCustomGameMessageResponseMessage()
	{
	}

	public RegisterCustomGameMessageResponseMessage(bool shouldReportActivities)
	{
		ShouldReportActivities = shouldReportActivities;
	}
}
