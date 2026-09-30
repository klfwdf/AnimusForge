using System;
using System.Collections.Generic;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge.Refactor.Modules;

internal sealed class CivilWarPanelFaction
{
	public string Name = "";
	public string Demand = "";
	public string Members = "";
	public string Color = "#8C1E1EFF";
	public int GrievanceBar;
	public bool Satisfied;
}

internal sealed class CivilWarPanelKingdom
{
	public string Name = "";
	public string Stage = "";
	public string GrievanceText = "";
	public int GrievanceBar;
	public List<CivilWarPanelFaction> Factions = new List<CivilWarPanelFaction>();
}

// Same-DLL owner for staged civil wars. Host code keeps only these calls.
internal interface ICivilWarModulePort
{
	void Load(string json);
	string Save();
	void AdvanceWeek(Kingdom kingdom, int weekIndex, int stability, Action<Kingdom, int> adjustStability, IReadOnlyList<string> recentEvents);
	void RecordGrievance(Kingdom kingdom, string sourceId, IEnumerable<Clan> clans, float points, int week, string text);
	void RecordPolicyImposed(Kingdom kingdom, string policyId, int week, string text);
	bool HasTrackedKingdom(Kingdom kingdom);
	int GetSettlementLoyaltyDelta(Settlement settlement);
	bool BlocksNewOffensiveWar(Kingdom kingdom);
	void ApplyPrestigeDelta(string kingdomId, int delta, string reason);
	IReadOnlyList<CivilWarPanelKingdom> GetPanelKingdoms(int pageIndex, int pageSize, out int pageCount);
	List<PostprocessRuleEntry> BuildPostprocessRules();
	bool TryApplyTag(Hero speaker, string tag, out string message);
	void NotifyRebelKingdomCreated(string factionId, Kingdom rebelKingdom, int week);
}
