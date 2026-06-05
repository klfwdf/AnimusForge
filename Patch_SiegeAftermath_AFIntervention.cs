using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;

namespace AnimusForge;

internal static class SiegeAftermathPatchBootstrap
{
	internal static void Apply(Harmony harmony)
	{
		if (harmony == null)
		{
			return;
		}
		PatchOne(harmony, typeof(Patch_GameMenu_SwitchToMenu_AFResolvedSiegeAftermath));
		PatchOne(harmony, typeof(Patch_SiegeAftermath_MenuTaken_OnInit_AFRedirect));
		PatchOne(harmony, typeof(Patch_SiegeAftermath_PlayerLeader_OnInit_AFRedirect));
		PatchOne(harmony, typeof(Patch_SiegeAftermath_ContextualSummary_OnInit_AFRedirect));
		PatchOne(harmony, typeof(Patch_SiegeAftermath_Continue_AFMassacreLoot));
		PatchOne(harmony, typeof(Patch_GameStateManager_OnTick_AFMassacreLoot));
	}

	private static void PatchOne(Harmony harmony, Type patchType)
	{
		try
		{
			harmony.CreateClassProcessor(patchType).Patch();
			Logger.LogTrace("SubModule", ">>> " + patchType.Name + " applied.");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("SubModule", ">>> " + patchType.Name + " failed: " + ex.Message);
		}
	}
}

[HarmonyPatch(typeof(GameMenu), "SwitchToMenu")]
public static class Patch_GameMenu_SwitchToMenu_AFResolvedSiegeAftermath
{
	public static bool Prefix(ref string menuId)
	{
		try
		{
			if (SiegeAiInterventionBehavior.TryHandleDirectMassacreAftermathMenuForExternal(menuId, "GameMenu.SwitchToMenu:" + menuId))
			{
				return false;
			}
			if (SiegeAiInterventionBehavior.TryHandleDirectPlunderAftermathMenuForExternal(menuId, "GameMenu.SwitchToMenu:" + menuId))
			{
				return false;
			}
			if (SiegeAiInterventionBehavior.ShouldRedirectResolvedAftermathMenuForExternal(menuId))
			{
				Logger.LogTrace("UI_Intercept", $"Skipping native siege aftermath SwitchToMenu '{menuId}' after AF resolution and finishing encounter.");
				SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("GameMenu.SwitchToMenu:" + menuId);
				return false;
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("UI_Intercept", "[ERROR] SwitchToMenu AF siege aftermath guard: " + ex);
		}
		return true;
	}
}

[HarmonyPatch(typeof(SiegeAftermathCampaignBehavior), "menu_settlement_taken_on_init")]
public static class Patch_SiegeAftermath_MenuTaken_OnInit_AFRedirect
{
	public static bool Prefix()
	{
		return !SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("menu_settlement_taken_on_init");
	}
}

[HarmonyPatch(typeof(SiegeAftermathCampaignBehavior), "menu_settlement_taken_player_leader_on_init")]
public static class Patch_SiegeAftermath_PlayerLeader_OnInit_AFRedirect
{
	public static bool Prefix()
	{
		return !SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("menu_settlement_taken_player_leader_on_init");
	}
}

[HarmonyPatch(typeof(SiegeAftermathCampaignBehavior), "siege_aftermath_contextual_summary_on_init")]
public static class Patch_SiegeAftermath_ContextualSummary_OnInit_AFRedirect
{
	public static bool Prefix()
	{
		return !SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("siege_aftermath_contextual_summary_on_init");
	}
}

[HarmonyPatch(typeof(SiegeAftermathCampaignBehavior), "menu_settlement_taken_continue_on_consequence")]
public static class Patch_SiegeAftermath_Continue_AFMassacreLoot
{
	public static bool Prefix()
	{
		return !SiegeAiInterventionBehavior.TryHandleNativeAftermathSummaryContinueForExternal("menu_settlement_taken_continue_on_consequence");
	}
}

[HarmonyPatch(typeof(GameStateManager), "OnTick")]
public static class Patch_GameStateManager_OnTick_AFMassacreLoot
{
	public static void Postfix(float dt)
	{
		try
		{
			SiegeAiInterventionBehavior.TryPumpDirectMassacreAftermathScriptForExternal("GameStateManager.OnTick");
			SiegeAiInterventionBehavior.TryPumpDirectPlunderAftermathScriptForExternal("GameStateManager.OnTick");
		}
		catch (Exception ex)
		{
			Logger.LogTrace("UI_Intercept", "[ERROR] GameStateManager AF aftermath loot pump: " + ex.Message);
		}
	}
}
