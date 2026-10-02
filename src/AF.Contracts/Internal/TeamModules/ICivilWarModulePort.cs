using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.Refactor.Modules;

internal enum CivilWarAction { JoinCrown, JoinOpposition, Leave, Found, Detonate, Suppress, Negotiate, Concede, ForceDissolve, Respond, DissolveOwn, ChangeDemand }
internal enum CivilWarActionStatus { Applied, Rejected, AwaitingPlayer, AwaitingKingdom, PartialFailure }
internal sealed class CoupCivilWarRegistration
{
    public string CoupId;
    public string KingdomId;
    public string RebelKingdomId;
    public string LeaderClanId;
    public string FormerKingId;
    public string FormerClanId;
    public bool RestoreDynasty;
    public string OriginalName;
    public string OriginalShortName;
}
internal sealed class CivilWarActionRequest
{
	public string OperationId = "";
	public string KingdomId = "";
	public string FactionId = "";
	public int Version = -1;
	public CivilWarAction Action;
	public string DemandId = "";
	public string TargetId = "";
	public int OfferTier = 1;
	public bool OfferInfluence;
	public bool Accept;
}
internal sealed class CivilWarActionQuote
{
	public bool Allowed;
	public string Reason = "";
	public string Consequence = "";
	public int Gold;
	public int Influence;
	public int CooldownUntilDay;
	public int Version;
}
internal sealed class CivilWarActionResult
{
	public CivilWarActionStatus Status;
	public string Message = "";
	public string FactionId = "";
}
internal sealed class CivilWarFoundingOption
{
	public string DemandId = "";
	public string TargetId = "";
	public string Text = "";
}

// Who the player is in their own kingdom's politics; picks the panel's action set.
internal enum CivilWarPanelRole { None, King, Crown, Middle, Member, Leader }

// One clan row in the faction panel (faction members, crown side, undecided, or the grievance ledger).
internal sealed class CivilWarPanelClan
{
	public string Name = "";
	public string Stance = "";
	public string Reason = "";
	public int Grievance;
	// Unrounded total grievance (0..100), shown with one decimal in the ledger.
	public float GrievanceValue;
	public int RelationToKing;
	public string RelationText = "";
	// Share of the kingdom's political strength, 0..100.
	public int PowerPercent;
	public bool IsLeader;
	public bool IsPlayer;
}

// Four-step demand progress: 0 = not reached, 1 = done, 2 = current.
internal sealed class CivilWarPanelStep
{
	public string Name = "";
	public string Note = "";
	public int State;
}

// Formation condition shown while the kingdom has no faction.
internal sealed class CivilWarPanelCondition
{
	public string Name = "";
	public string Value = "";
	public bool Met;
}

// One faction dossier: each faction has its own demand, stage and grievance.
internal sealed class CivilWarPanelFaction
{
	public string Id = "";
	public string Tag = "";
	public string DemandTag = "";
	public string DemandId = "";
	public string TargetId = "";
	public string Name = "";
	public string ShortName = "";
	public string Color = "#8C1E1EFF";
	// Full stage line (kept for logs and tests).
	public string Stage = "";
	public string LeaderLine = "";
	public string DemandTitle = "";
	public string Note = "";
	public int Grievance;
	public int PowerPercent;
	public int Fortifications;
	public int Refusals;
	public string RefusalUnit = "";
	// Right-hand deadline block of the dossier header.
	public string DeadlineLabel = "";
	public string DeadlineValue = "";
	public string DeadlineUnit = "";
	public string DeadlineNote = "";
	public bool DeadlineUrgent;
	public List<CivilWarPanelStep> Steps = new List<CivilWarPanelStep>();
	public bool IsPlayerFaction;
	public bool IsPlayerLeader;
	// Royal reply addressed to this faction's leader (player-led factions answer from the panel).
	public bool HasPendingResponse;
	public bool PendingDissolve;
	public int PendingGold;
	public int PendingInfluence;
	public int MemberCount;
	// All members: leader first, then the player, then by grievance. The panel pages them.
	public List<CivilWarPanelClan> Members = new List<CivilWarPanelClan>();
}

// The player's own kingdom only. Built on demand when the kingdom screen tab opens.
internal sealed class CivilWarPanelKingdom
{
	public string KingdomId = "";
	public string Identity = "";
	public long Revision;
	public bool Available;
	public string EmptyText = "";
	public string Name = "";
	public string StageText = "";
	public int Stability;
	public string StabilityTier = "";
	public CivilWarPanelRole Role;
	public string PlayerClanName = "";
	public string PlayerFactionId = "";
	public int PlayerGrievance;
	public int PlayerRelationToKing;
	public int PlayerRelationToLeader;
	public string ActionHint = "";
	public int OppositionCount;
	public int CrownCount;
	public int MiddleCount;
	public int CrownPowerPercent;
	public int MiddlePowerPercent;
	// Formation threshold view (most aggrieved eligible vassal outside any faction).
	public float TopGrievance;
	public string TopGrievanceClan = "";
	public int Threshold;
	public int MaxFactions;
	public List<CivilWarPanelCondition> Conditions = new List<CivilWarPanelCondition>();
	// Newest first, already prefixed with the week.
	public List<string> Chronicle = new List<string>();
	public List<CivilWarPanelFaction> Factions = new List<CivilWarPanelFaction>();
	// Full lists (not trimmed); the panel pages them.
	public List<CivilWarPanelClan> Crown = new List<CivilWarPanelClan>();
	public List<CivilWarPanelClan> Middle = new List<CivilWarPanelClan>();
}

// Same-DLL owner for staged civil wars. Host code keeps only these calls.
internal interface ICivilWarModulePort
{
    bool CanTrackCoupWar(Kingdom kingdom);
    bool TryRegisterCoupWar(CoupCivilWarRegistration registration, out string message);
	long Revision { get; }
	event Action StateChanged;
	List<CivilWarFoundingOption> GetFoundingOptions(string kingdomId);
	CivilWarActionQuote Quote(CivilWarActionRequest request);
	CivilWarActionResult Execute(CivilWarActionRequest request);
	void NotifyPoliticalChange(Kingdom kingdom, string sourceId);
	void ProcessPending();
	bool TryTakePoliticalResponse(out CivilWarActionRequest request, out string text);
	void Load(string json);
	string Save();
	void AdvanceDay(int dayIndex);
	void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> recentEvents);
	void RecordGrievance(Kingdom kingdom, string sourceId, IEnumerable<Clan> clans, float points, int week, string text);
	void RecordPolicyImposed(Kingdom kingdom, string policyId, int week, string text);
	bool HasTrackedKingdom(Kingdom kingdom);
	int GetSettlementLoyaltyDelta(Settlement settlement);
	bool BlocksNewOffensiveWar(Kingdom kingdom);
	void ApplyPrestigeDelta(string kingdomId, int delta, string reason);
	CivilWarPanelKingdom GetPlayerKingdomPanel();
	List<PostprocessRuleEntry> BuildPostprocessRules();
	bool TryApplyTag(Hero speaker, string tag, out string message);
	void NotifyRebelKingdomCreated(string factionId, Kingdom rebelKingdom, int week);
	// Host asks before naming/executing a queued civil-war rebellion; false means the faction moved on.
	bool IsRebellionRequestActive(string factionId);
	// Host gave up on a queued civil-war rebellion (failed, skipped, immunity).
	void NotifyRebellionFailed(string factionId, string reason);
	void RecordPeace(Kingdom kingdom, IFaction other, int week);
	bool IsCivilWarPair(Kingdom a, Kingdom b);
	bool HasPendingPlayerUltimatumPrompt { get; }
	bool TryTakePlayerUltimatumPrompt(out string factionId, out string text);
	bool AnswerPlayerUltimatum(string factionId, bool accept, out string message);
	// Player vassal whose faction just raised a rebel kingdom: follow the rebels or stay with the crown.
	bool HasPendingFollowPrompt { get; }
	bool TryTakeFollowPrompt(out string factionId, out string text);
	bool AnswerFollow(string factionId, bool follow, out string message);
}
