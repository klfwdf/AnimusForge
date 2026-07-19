using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge;

internal static class CastleAftermathConstructionSpeedPatchRegistrar
{
	internal static void Register(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		TryPatch(harmony, typeof(CastleAftermathDailyConstructionPowerPatch));
		TryPatch(harmony, typeof(CastleAftermathConstructionPowerWithoutBoostPatch));
	}

	private static void TryPatch(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", patchType.Name + " init failed: " + ex.Message);
		}
	}
}

[HarmonyPatch(typeof(DefaultBuildingConstructionModel), nameof(DefaultBuildingConstructionModel.CalculateDailyConstructionPower))]
internal static class CastleAftermathDailyConstructionPowerPatch
{
	private static readonly TextObject RepairLaborText = new TextObject("{=!}战俘修缮城堡");

	private static void Postfix(Town town, ref ExplainedNumber __result)
	{
		float bonus = CastleAftermathSettlementRuntimeBridge.GetActiveConstructionSpeedBonus(town);
		if (bonus > 0.0001f)
		{
			__result.AddFactor(bonus, RepairLaborText);
		}
	}
}

[HarmonyPatch(typeof(DefaultBuildingConstructionModel), nameof(DefaultBuildingConstructionModel.CalculateDailyConstructionPowerWithoutBoost))]
internal static class CastleAftermathConstructionPowerWithoutBoostPatch
{
	private static void Postfix(Town town, ref int __result)
	{
		float bonus = CastleAftermathSettlementRuntimeBridge.GetActiveConstructionSpeedBonus(town);
		if (bonus > 0.0001f && __result > 0)
		{
			__result = Math.Max(0, (int)Math.Round(__result * (1d + bonus), MidpointRounding.AwayFromZero));
		}
	}
}
