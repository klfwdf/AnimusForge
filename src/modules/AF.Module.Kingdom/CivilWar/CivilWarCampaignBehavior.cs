using System;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
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
	}

	public override void SyncData(IDataStore dataStore)
	{
	}

	private void OnSessionLaunched(CampaignGameStarter starter)
	{
		if (_loyaltyPatched) return;
		MethodInfo method = AccessTools.Method(typeof(AnimusForgeSettlementLoyaltyModel), nameof(AnimusForgeSettlementLoyaltyModel.CalculateLoyaltyChange));
		if (method == null) return;
		new Harmony("AnimusForge.civilwar.loyalty").Patch(method, postfix: new HarmonyMethod(typeof(CivilWarCampaignBehavior), nameof(LoyaltyPostfix)));
		_loyaltyPatched = true;
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
		MyBehavior.Instance?.CaptureWorldBulletinCivilWar(kingdom.StringId, (kingdom.StringId ?? "") + ":" + Math.Max(0, weekIndex));
	}
}
