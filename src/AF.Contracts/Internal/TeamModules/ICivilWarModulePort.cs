using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.Refactor.Modules;

internal enum CivilWarAction { JoinCrown, JoinOpposition, Leave, Found, Detonate, Suppress, Negotiate, Concede, ForceDissolve, Respond }
internal enum CivilWarActionStatus { Applied, Rejected, AwaitingPlayer, AwaitingKingdom, PartialFailure }
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

// One clan row in the faction panel.
internal sealed class CivilWarPanelClan
{
	public string Name = "";
	public string Info = "";
	public int Grievance;
	public bool IsLeader;
}

// One faction column: each faction has its own demand, stage and grievance.
internal sealed class CivilWarPanelFaction
{
	public string Id = "";
	public string Tag = "";
	public string Name = "";
	public string Color = "#8C1E1EFF";
	public string Stage = "";
	public string Leader = "";
	public string Demand = "";
	public string Goal = "";
	public string Power = "";
	public string Refusal = "";
	public int Grievance;
	// Total member count; Members may be trimmed for display.
	public int MemberCount;
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
	public int OppositionCount;
	public int CrownCount;
	public int MiddleCount;
	public List<CivilWarPanelFaction> Factions = new List<CivilWarPanelFaction>();
	public List<CivilWarPanelClan> Crown = new List<CivilWarPanelClan>();
	public List<CivilWarPanelClan> Middle = new List<CivilWarPanelClan>();
}

// Same-DLL owner for staged civil wars. Host code keeps only these calls.
internal interface ICivilWarModulePort
{
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
