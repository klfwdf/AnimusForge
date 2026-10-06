using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TaleWorlds.Diamond;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.MountAndBlade.Diamond.Ranked;

namespace Messages.FromLobbyServer.ToClient;

[Serializable]
[MessageDescription("LobbyServer", "Client", true)]
public class BattleOverMessage : Message
{
	[JsonProperty]
	public int OldExperience { get; private set; }

	[JsonProperty]
	public int NewExperience { get; private set; }

	[JsonProperty]
	public List<string> EarnedBadges { get; private set; }

	[JsonProperty]
	public int GoldGained { get; private set; }

	[JsonProperty]
	public RankBarInfo OldInfo { get; private set; }

	[JsonProperty]
	public RankBarInfo NewInfo { get; private set; }

	[JsonProperty]
	public BattleCancelReason BattleCancelReason { get; private set; }

	public BattleOverMessage()
	{
	}

	public BattleOverMessage(int oldExperience, int newExperience, List<string> earnedBadges, int goldGained, BattleCancelReason battleCancelReason = BattleCancelReason.None)
	{
		OldExperience = oldExperience;
		NewExperience = newExperience;
		EarnedBadges = earnedBadges;
		GoldGained = goldGained;
		BattleCancelReason = battleCancelReason;
	}

	public BattleOverMessage(BattleCancelReason battleCancelReason)
	{
		BattleCancelReason = battleCancelReason;
	}
}
