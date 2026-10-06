using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlayerServices;

namespace Messages.FromLobbyServer.ToLobbyServer;

[Serializable]
[MessageDescription("CustomBattleServerManager", "CustomBattleServerManager", true)]
public class ProcessBadgesAndStatsAfterCustomBattleServerFinishMessage : Message
{
	[JsonProperty]
	public List<BadgeDataEntry> BadgeDataEntries { get; private set; }

	[JsonProperty]
	public PlayerId[] PlayerIds { get; private set; }

	public ProcessBadgesAndStatsAfterCustomBattleServerFinishMessage()
	{
	}

	public ProcessBadgesAndStatsAfterCustomBattleServerFinishMessage(List<BadgeDataEntry> badgeDataEntries, PlayerId[] playerIds)
	{
		BadgeDataEntries = badgeDataEntries;
		PlayerIds = playerIds;
	}
}
