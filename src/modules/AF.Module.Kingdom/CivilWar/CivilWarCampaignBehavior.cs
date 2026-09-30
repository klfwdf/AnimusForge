using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using AnimusForge.Refactor.Modules;

namespace AnimusForge;

// Thin campaign host. Rules stay in KingdomCivilWarOwner; other domains are not edited.
internal sealed class CivilWarCampaignBehavior : CampaignBehaviorBase
{
	private static readonly TextObject LoyaltyText = new TextObject("内战反对派领地");
	private bool _loyaltyPatched;

	public override void RegisterEvents()
	{
		CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
		CampaignEvents.HeroKilledEvent.AddNonSerializedListener(this, OnHeroKilled);
		CampaignEvents.WarDeclared.AddNonSerializedListener(this, OnWarDeclared);
		CampaignEvents.MakePeace.AddNonSerializedListener(this, OnMakePeace);
		CampaignEvents.VillageLooted.AddNonSerializedListener(this, OnVillageLooted);
		CampaignEvents.OnSettlementOwnerChangedEvent.AddNonSerializedListener(this, OnSettlementOwnerChanged);
		CampaignEvents.OnClanChangedKingdomEvent.AddNonSerializedListener(this, OnClanChangedKingdom);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
	}

	// Flag check only; the scan runs once per ultimatum and the inquiry waits until no other popup is open.
	private void OnHourlyTick()
	{
		if (!TeamModuleServices.CivilWar.HasPendingPlayerUltimatumPrompt || InformationManager.IsAnyInquiryActive()) return;
		if (!TeamModuleServices.CivilWar.TryTakePlayerUltimatumPrompt(out string kingdomId, out string text)) return;
		InformationManager.ShowInquiry(new InquiryData("反对派最后通牒", text, true, true, "接受诉求", "拒绝", () => Answer(kingdomId, true), () => Answer(kingdomId, false)), true);
	}

	private static void Answer(string kingdomId, bool accept)
	{
		TeamModuleServices.CivilWar.AnswerPlayerUltimatum(kingdomId, accept, out string message);
		if (!string.IsNullOrWhiteSpace(message)) InformationManager.DisplayMessage(new InformationMessage(message));
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		List<string> catalogErrors = CivilWarCatalog.Initialize(CivilWarEffects.Ids);
		foreach (string error in catalogErrors) Logger.Log("KingdomCivilWar", "[WARN] catalog " + error);
		if (_loyaltyPatched) return;
		MethodInfo method = AccessTools.Method(typeof(AnimusForgeSettlementLoyaltyModel), nameof(AnimusForgeSettlementLoyaltyModel.CalculateLoyaltyChange));
		if (method == null) return;
		new Harmony("AnimusForge.civilwar.loyalty").Patch(method, postfix: new HarmonyMethod(typeof(CivilWarCampaignBehavior), nameof(LoyaltyPostfix)));
		_loyaltyPatched = true;
	}

	private static int Week() { return CivilWarWorld.CurrentWeek(); }
	private static void Add(Kingdom kingdom, string source, IEnumerable<Clan> clans, float points, string text)
	{
		if (kingdom == null) return;
		TeamModuleServices.CivilWar.RecordGrievance(kingdom, source, clans, points, Week(), text);
	}
	private static IEnumerable<Clan> Vassals(Kingdom kingdom) { return CivilWarWorld.Vassals(kingdom).ToList(); }

	private void OnHeroKilled(Hero victim, Hero killer, KillCharacterAction.KillCharacterActionDetail detail, bool showNotification)
	{
		Kingdom kingdom = victim?.Clan?.Kingdom;
		if (kingdom == null || victim.Clan == kingdom.RulingClan || (detail != KillCharacterAction.KillCharacterActionDetail.Executed && detail != KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)) return;
		// Only the crown's own executions are a royal grievance; enemies executing a vassal are not.
		if (killer == null || killer.Clan != kingdom.RulingClan) return;
		Add(kingdom, "royal_execution", new[] { victim.Clan }, 25f, "王室处决了" + CivilWarWorld.ClanName(victim.Clan) + "的成员");
	}

	// Only wars the crown chose; rebellions, kingdom creation and crime-driven wars are not imposed on the vassals.
	private void OnWarDeclared(IFaction first, IFaction second, DeclareWarAction.DeclareWarDetail detail)
	{
		if (detail != DeclareWarAction.DeclareWarDetail.Default && detail != DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision) return;
		if (first is Kingdom kingdom && second is Kingdom && !IsCivilWarPair(kingdom, second)) Add(kingdom, "war_imposed", Vassals(kingdom), 8f, "王国被迫开战");
	}

	private void OnMakePeace(IFaction first, IFaction second, MakePeaceAction.MakePeaceDetail detail)
	{
		if (first is Kingdom k1 && second is Kingdom k2 && !IsCivilWarPair(k1, k2))
		{
			Add(k1, "peace_imposed", Vassals(k1), 7f, "王国被迫议和");
			Add(k2, "peace_imposed", Vassals(k2), 7f, "王国被迫议和");
		}
		if (first is Kingdom a) TeamModuleServices.CivilWar.RecordPeace(a, second, Week());
		if (second is Kingdom b) TeamModuleServices.CivilWar.RecordPeace(b, first, Week());
	}

	private void OnVillageLooted(Village village)
	{
		Kingdom kingdom = village?.Settlement?.OwnerClan?.Kingdom;
		Clan owner = village?.Settlement?.OwnerClan;
		if (kingdom != null && owner != null) Add(kingdom, "lands_raided", new[] { owner }, 20f, CivilWarWorld.ClanName(owner) + "的领地遭到劫掠");
	}

	private void OnSettlementOwnerChanged(Settlement settlement, bool openNewOwner, Hero newOwner, Hero oldOwner, Hero capturer, ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail detail)
	{
		Clan oldClan = oldOwner?.Clan;
		Kingdom kingdom = oldClan?.Kingdom;
		if (kingdom == null || (settlement != null && !settlement.IsTown && !settlement.IsCastle)) return;
		// Transfers inside the kingdom (grants, gifts, barter) and leaving the kingdom are not a lost fief.
		if (newOwner?.Clan?.Kingdom == kingdom || detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByLeaveFaction
			|| detail == ChangeOwnerOfSettlementAction.ChangeOwnerOfSettlementDetail.ByClanDestruction) return;
		Add(kingdom, "fief_lost", new[] { oldClan }, 20f, CivilWarWorld.ClanName(oldClan) + "失去封地");
	}

	private static bool IsCivilWarPair(Kingdom kingdom, IFaction other)
	{
		Kingdom otherKingdom = other as Kingdom;
		return TeamModuleServices.CivilWar.IsCivilWarPair(kingdom, otherKingdom);
	}

	// Vanilla removes the clan from its kingdom before OnClanDestroyed, so the old kingdom only exists here.
	private void OnClanChangedKingdom(Clan clan, Kingdom oldKingdom, Kingdom newKingdom, ChangeKingdomAction.ChangeKingdomActionDetail detail, bool showNotification)
	{
		if (detail != ChangeKingdomAction.ChangeKingdomActionDetail.LeaveByClanDestruction || oldKingdom == null || clan == oldKingdom.RulingClan) return;
		Add(oldKingdom, "fief_lost", Vassals(oldKingdom), 10f, CivilWarWorld.ClanName(clan) + "被消灭");
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
	{
		Kingdom kingdom = decision?.Kingdom;
		if (kingdom == null) return;
		if (decision is SettlementClaimantDecision claimant)
		{
			RecordFiefDenied(kingdom, claimant, outcome as SettlementClaimantDecision.ClanAsDecisionOutcome);
			return;
		}
		// Only enforced policy decisions count here; war/peace grievance comes from WarDeclared/MakePeace.
		KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
		if (policyDecision == null || !(outcome is KingdomPolicyDecision.PolicyDecisionOutcome policyOutcome) || !policyOutcome.ShouldDecisionBeEnforced) return;
		TeamModuleServices.CivilWar.RecordPolicyImposed(kingdom, policyDecision.Policy?.StringId ?? "", Week(), "王国强推政策：" + (policyDecision.Policy?.Name?.ToString() ?? "现行政策"));
	}

	// Losing claimants who actually backed themselves; neutral voters are not denied anything.
	private static void RecordFiefDenied(Kingdom kingdom, SettlementClaimantDecision decision, SettlementClaimantDecision.ClanAsDecisionOutcome winner)
	{
		if (winner?.Clan == null || decision.Settlement == null) return;
		List<Clan> denied = new List<Clan>();
		foreach (DecisionOutcome candidate in decision.DetermineInitialCandidates())
		{
			Clan clan = (candidate as SettlementClaimantDecision.ClanAsDecisionOutcome)?.Clan;
			if (clan == null || clan == winner.Clan || clan == kingdom.RulingClan) continue;
			if (winner.SupporterList.Any(s => s?.Clan == clan && s.SupportWeight != Supporter.SupportWeights.StayNeutral)) continue;
			if (clan.Settlements.Count(x => x.IsTown || x.IsCastle) > 1) continue;
			denied.Add(clan);
		}
		if (denied.Count > 0) Add(kingdom, "fief_denied", denied, 12f, decision.Settlement.Name + "封给了" + CivilWarWorld.ClanName(winner.Clan));
	}

	private static void LoyaltyPostfix(Town town, ref ExplainedNumber __result)
	{
		int delta = TeamModuleServices.CivilWar.GetSettlementLoyaltyDelta(town?.Settlement);
		if (delta != 0) __result.Add(delta, LoyaltyText, null);
	}

	internal static void RecordMaterial(Kingdom kingdom, int weekIndex, string text)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(text)) return;
		MyBehavior.RecordEventSourceMaterialForExternal("civil_war", "内战 - " + (kingdom.Name?.ToString() ?? "王国"), text, "civil_war:" + (kingdom.StringId ?? "") + ":" + Math.Max(0, weekIndex), kingdom.StringId ?? "", true, true);
	}
}
