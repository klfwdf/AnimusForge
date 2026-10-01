using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace AnimusForge;

// Save model v4 (payload under the existing `_af_kingdom_civil_war_v2` key). A kingdom holds several concurrent factions, each with its own
// demand, grievance, ultimatum and war. Only ids and numbers are stored; catalog content is looked up by id so
// removing a catalog entry dissolves affected factions on load instead of failing. v2 saves (single `Faction`)
// are migrated into `Factions` by the owner's Sanitize.
internal enum KingdomCivilWarStage
{
	None = 0,
	Discontent = 1,     // kingdom: grievance accumulating, no faction
	FactionFormed = 2,  // faction recruiting, ultimatum scheduled
	Ultimatum = 3,      // faction waiting for a player king's answer
	OpenWar = 4,        // faction's rebel kingdom requested or at war
	Cooldown = 5        // v2 only: kingdom-wide cooldown (v3 keeps CooldownUntilWeek for new factions)
}

internal enum KingdomCivilWarSide
{
	Crown = 0,
	Opposition = 1,     // member of the faction in FactionId
	Middle = 2
}

internal sealed class KingdomCivilWarClanState
{
	[JsonIgnore] internal KingdomCivilWarKingdomState Owner;
	[JsonIgnore] internal KingdomCivilWarFactionState CachedFaction;
	[JsonIgnore] internal float CachedGrievance;
	[JsonIgnore] internal float CachedStrength;
	public int MembershipEvaluationDay = -1;
	public int LastDecayDay = -1;
	public int SideSinceDay = -1;
	public string ClanId = "";
	public KingdomCivilWarSide Side = KingdomCivilWarSide.Middle;
	public string FactionId = "";
	public int SideSinceWeek;
	public int LastRecruitWeek;
	// grievance source id -> points (decays daily, preserving the source's seven-day DecayPerWeek)
	public Dictionary<string, float> Grievance = new Dictionary<string, float>(StringComparer.Ordinal);
}

internal sealed class KingdomCivilWarFactionState
{
	[JsonIgnore] internal float GrievanceSum;
	[JsonIgnore] internal float CachedStrength;
	[JsonIgnore] internal int CachedMemberCount;
	[JsonIgnore] internal List<string> LastParticipantIds = new List<string>();
	public bool EscalationPending;
	public bool CrossedSeventy;
	public int WarEvaluationDay = -1;
	public int Version;
	public bool PlayerFounded;
	public int UltimatumDay = -1;
	public int AnswerDeadlineDay = -1;
	public int WarRequestDay = -1;
	public int WarStartDay = -1;
	public int GovernanceUntilDay;
	public int ProposalUntilDay;
	public int LastDemandDay = -1;
	public int LastEscalationDay = -1;
	public bool SuppressionExtended;
	public bool DemandLocked;
	public CivilWarPendingResponse PendingResponse;
	public string PendingConcessionOperationId = "";
	public string PendingConcessionRulerId = "";
	public string PendingConcessionLeaderId = "";
	public string RebellionOperationId = "";
	public string Id = "";
	public KingdomCivilWarStage Stage = KingdomCivilWarStage.FactionFormed;
	public int StageWeek;
	public string DemandId = "";
	public string LeaderClanId = "";
	public string TargetId = "";
	public string TargetName = "";
	public int CreatedWeek;
	public int UltimatumWeek;
	public int Refusals;
	public int Defers;
	// Mean total grievance of member clans (0..100), updated by events and daily decay.
	public float Grievance;
	public bool PlayerAnswerPending;
	public bool PlayerPrompted;
	public int PlayerAnswerDeadlineWeek;
	public int WarGoal;
	public bool PlayerFollowPending;
	public bool PlayerFollowAccepted;
	public float LastEscalateChance;
	public string LastRuling = "";
	// Single-war mode: escalation passed while another faction of the kingdom was at war.
	public bool WaitingForOtherWar;
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
	// Persist the chosen settlement across retries; never reroll after game effects have started.
	public string ResolutionOutcomeId = "";
	public string ResolutionError = "";
	public bool ResolutionNeedsReview;
	public bool ResolutionReturnCompleted;
	// A coup-origin war may fight for the displaced dynasty; ordinary wars leave these empty.
	public string CoupId = "";
	public string RestorationClanId = "";
	public string RestorationHeroId = "";
	public string RestorationKingdomName = "";
	public string RestorationKingdomShortName = "";
	// Set only by the war-result owner before its own peace/return effects run.
	public bool RestorationVictoryConfirmed;
}

internal sealed class KingdomCivilWarHistoryEntry
{
	public int Week;
	public string Text = "";
}

internal sealed class KingdomCivilWarKingdomState
{
	[JsonIgnore] internal float CachedGrievance;
	public int FormationAttemptDay = -1;
	public int FormationDueDay = -1;
	public int CooldownUntilDay = -1;
	public int AftermathUntilDay = -1;
	public int NoPeaceUntilDay = -1;
	public string KingdomId = "";
	// Kingdom-level summary: Discontent when no faction exists; otherwise the most advanced faction stage.
	public KingdomCivilWarStage Stage = KingdomCivilWarStage.Discontent;
	public int StageWeek;
	public int LastAdvancedWeek;
	// -1 means a pre-daily-decay save: establish today's baseline without retroactive decay.
	public int LastGrievanceDecayDay = -1;
	// No new faction forms before this week (set when a faction finishes). Existing factions keep running.
	public int CooldownUntilWeek;
	// Mood of the remaining factions after the last civil war (CivilWarAftermath), valid through AftermathUntilWeek.
	public int Aftermath;
	public int AftermathUntilWeek;
	public int FactionSerial;
	public string PlayerSide = "";
	public string LastImposedPolicyId = "";
	public float LastMaxGrievance;
	// continue_war pledge: breaking it adds broken_pledge grievance to these clans
	public string NoPeaceTargetId = "";
	public int NoPeaceUntilWeek;
	public List<string> NoPeaceClanIds = new List<string>();
	public Dictionary<string, KingdomCivilWarClanState> Clans = new Dictionary<string, KingdomCivilWarClanState>(StringComparer.OrdinalIgnoreCase);
	public List<KingdomCivilWarFactionState> Factions = new List<KingdomCivilWarFactionState>();
	// v2 single faction; read for migration only, never written.
	[JsonProperty("Faction", NullValueHandling = NullValueHandling.Ignore)]
	public KingdomCivilWarFactionState LegacyFaction;
	public List<KingdomCivilWarHistoryEntry> History = new List<KingdomCivilWarHistoryEntry>();
}

internal sealed class KingdomCivilWarStorage
{
	public int Version = 4;
	public Dictionary<string, int> ClanExitUntilDay = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, CivilWarOperation> Operations = new Dictionary<string, CivilWarOperation>(StringComparer.Ordinal);
	public Dictionary<string, KingdomCivilWarKingdomState> Kingdoms = new Dictionary<string, KingdomCivilWarKingdomState>(StringComparer.OrdinalIgnoreCase);
}

internal sealed class CivilWarPendingResponse
{
	[JsonIgnore] internal bool Prompted;
	public bool Accepted;
	public string ResponseOperationId = "";
	public string OperationId = "";
	public string RulerClanId = "";
	public string LeaderClanId = "";
	public bool Dissolve;
	public int Gold;
	public int Influence;
	public int DeadlineDay;
}

// A receipt is saved before entering native code. Unknown partial effects are never blindly replayed.
internal sealed class CivilWarOperation
{
	public string Fingerprint = "";
	public string FactionId = "";
	public int Status;
	public string Message = "";
	public bool InProgress;
	public bool FactsWritten;
}
