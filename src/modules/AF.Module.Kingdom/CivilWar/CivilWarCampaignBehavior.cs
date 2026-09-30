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
		CampaignEvents.OnClanDestroyedEvent.AddNonSerializedListener(this, OnClanDestroyed);
		CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
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
		if (kingdom != null && (detail == KillCharacterAction.KillCharacterActionDetail.Executed || detail == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent)) Add(kingdom, "royal_execution", new[] { victim.Clan }, 25f, "王室处决了" + CivilWarWorld.ClanName(victim.Clan) + "的成员");
	}

	private void OnWarDeclared(IFaction first, IFaction second, DeclareWarAction.DeclareWarDetail detail)
	{
		Kingdom kingdom = first as Kingdom ?? second as Kingdom;
		if (kingdom != null) Add(kingdom, "war_imposed", Vassals(kingdom), 8f, "王国被迫开战");
	}

	private void OnMakePeace(IFaction first, IFaction second, MakePeaceAction.MakePeaceDetail detail)
	{
		Kingdom kingdom = first as Kingdom ?? second as Kingdom;
		if (kingdom != null) Add(kingdom, "peace_imposed", Vassals(kingdom), 7f, "王国被迫议和");
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
		if (kingdom != null && oldClan != null) Add(kingdom, "fief_lost", new[] { oldClan }, 20f, CivilWarWorld.ClanName(oldClan) + "失去封地");
	}

	private void OnClanDestroyed(Clan clan)
	{
		Kingdom kingdom = clan?.Kingdom;
		if (kingdom != null) Add(kingdom, "fief_lost", Vassals(kingdom), 20f, CivilWarWorld.ClanName(clan) + "被消灭");
	}

	private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome outcome, bool isPlayerInvolved)
	{
		Kingdom kingdom = decision?.Kingdom;
		if (kingdom == null) return;
		string decisionName = decision.GetType().Name;
		KingdomPolicyDecision policyDecision = decision as KingdomPolicyDecision;
		if (policyDecision != null && outcome is KingdomPolicyDecision.PolicyDecisionOutcome policyOutcome && policyOutcome.ShouldDecisionBeEnforced)
		{
			TeamModuleServices.CivilWar.RecordPolicyImposed(kingdom, policyDecision.Policy?.StringId ?? "", Week(), "王国强推政策：" + (policyDecision.Policy?.Name?.ToString() ?? "现行政策"));
			return;
		}
		string source = decisionName.IndexOf("DeclareWar", StringComparison.OrdinalIgnoreCase) >= 0 ? "war_imposed" : decision is MakePeaceKingdomDecision ? "peace_imposed" : "policy_imposed";
		Add(kingdom, source, Vassals(kingdom), 8f, "王国决议已执行：" + decision.GetType().Name);
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
