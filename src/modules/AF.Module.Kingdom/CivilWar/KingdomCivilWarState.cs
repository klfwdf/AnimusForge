using System;
using System.Collections.Generic;

namespace AnimusForge;

// Save model v2 (`_af_kingdom_civil_war_v2`). Only ids and numbers are stored; catalog content is
// looked up by id so removing a catalog entry dissolves affected factions on load instead of failing.
internal enum KingdomCivilWarStage
{
	None = 0,
	Discontent = 1,     // grievance accumulating, no faction yet
	FactionFormed = 2,  // faction recruiting, ultimatum scheduled
	Ultimatum = 3,      // waiting for a player king's answer
	OpenWar = 4,        // rebel kingdom requested or at war
	Cooldown = 5        // resolved, no new faction until CooldownUntilWeek
}

internal enum KingdomCivilWarSide
{
	Crown = 0,
	Opposition = 1,
	Middle = 2
}

internal sealed class KingdomCivilWarClanState
{
	public string ClanId = "";
	public KingdomCivilWarSide Side = KingdomCivilWarSide.Middle;
	public int SideSinceWeek;
	public int LastRecruitWeek;
	// grievance source id -> points (decays weekly by the source's DecayPerWeek)
	public Dictionary<string, float> Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
}

internal sealed class KingdomCivilWarFactionState
{
	public string Id = "";
	public string DemandId = "";
	public string LeaderClanId = "";
	public string TargetId = "";
	public string TargetName = "";
	public int CreatedWeek;
	public int UltimatumWeek;
	public int Refusals;
	public bool PlayerAnswerPending;
	public bool PlayerPrompted;
	public int PlayerAnswerDeadlineWeek;
	public int WarGoal;
	public bool PlayerFollowPending;
	public bool PlayerFollowAccepted;
	public float LastEscalateChance;
	public string LastRuling = "";
	// Open war
	public int WarRequestWeek;
	public string RebelKingdomId = "";
	public int WarStartWeek;
	public float RebelFortShareAtStart;
	public float LastWarScore;
	public float LastFactionPower;
	public bool EndedByPeace;
	public bool RebelKingdomDestroyed;
	public bool PlayerFollowAsked;
	public List<string> WarClanIds = new List<string>();
}

internal sealed class KingdomCivilWarHistoryEntry
{
	public int Week;
	public string Text = "";
}

internal sealed class KingdomCivilWarKingdomState
{
	public string KingdomId = "";
	public KingdomCivilWarStage Stage = KingdomCivilWarStage.Discontent;
	public int StageWeek;
	public int LastAdvancedWeek;
	public int CooldownUntilWeek;
	public int FactionSerial;
	public string PlayerSide = "";
	public string LastImposedPolicyId = "";
	public float LastMaxGrievance;
	// continue_war pledge: breaking it adds broken_pledge grievance to these clans
	public string NoPeaceTargetId = "";
	public int NoPeaceUntilWeek;
	public List<string> NoPeaceClanIds = new List<string>();
	public Dictionary<string, KingdomCivilWarClanState> Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
	public KingdomCivilWarFactionState Faction;
	public List<KingdomCivilWarHistoryEntry> History = new List<KingdomCivilWarHistoryEntry>();
}

internal sealed class KingdomCivilWarStorage
{
	public int Version = 2;
	public Dictionary<string, KingdomCivilWarKingdomState> Kingdoms = new Dictionary<string, KingdomCivilWarKingdomState>(StringComparer.OrdinalIgnoreCase);
}
