using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;

namespace Messages.FromBattleServer.ToBattleServerManager;

[Serializable]
[MessageDescription("BattleServer", "BattleServerManager", true)]
public class BattleStartedMessage : Message
{
	[JsonProperty]
	public bool Report { get; private set; }

	[JsonProperty]
	public Dictionary<string, int> PlayerTeams { get; private set; }

	public BattleStartedMessage()
	{
	}

	public BattleStartedMessage(bool report)
	{
		Report = report;
		PlayerTeams = null;
	}

	public BattleStartedMessage(bool report, Dictionary<string, int> playerTeams)
	{
		Report = report;
		PlayerTeams = playerTeams;
	}
}
