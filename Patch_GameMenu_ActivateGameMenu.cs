using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

[HarmonyPatch(typeof(GameMenu), "ActivateGameMenu")]
public static class Patch_GameMenu_ActivateGameMenu
{
	public static bool Prefix(ref string menuId)
	{
		try
		{
			if (SiegeAiInterventionBehavior.TryHandleDirectMassacreAftermathMenuForExternal(menuId, "GameMenu.ActivateGameMenu:" + (menuId ?? "N/A")))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation for direct GCCZ massacre aftermath. Menu=" + (menuId ?? "N/A"));
				return false;
			}
			if (SiegeAiInterventionBehavior.TryHandleDirectPlunderAftermathMenuForExternal(menuId, "GameMenu.ActivateGameMenu:" + (menuId ?? "N/A")))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation for direct GCCZ plunder aftermath. Menu=" + (menuId ?? "N/A"));
				return false;
			}
			if (SiegeAiInterventionBehavior.ShouldRedirectResolvedAftermathMenuForExternal(menuId))
			{
				Logger.LogTrace("UI_Intercept", "Skipping native siege aftermath menu after GCCZ resolution and finishing encounter. Menu=" + (menuId ?? "N/A"));
				SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("GameMenu.ActivateGameMenu:" + (menuId ?? "N/A"));
				return false;
			}
			if (SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuActivationForExternal(menuId))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation after GCCZ intervention. Menu=" + (menuId ?? "N/A"));
				return false;
			}
			if (!(menuId == "encounter"))
			{
				return true;
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter)
			{
				Logger.LogTrace("UI_Intercept", "Native encounter leave is pending; keep native 'encounter' menu so PlayerEncounter.Finish can run.");
				return true;
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.PlayerSurrender)
			{
				Logger.LogTrace("UI_Intercept", "Native player surrender is pending; keep native 'encounter' menu so surrender result can resolve.");
				return true;
			}
			if (LordEncounterBehavior.HasPendingMeetingBattleVictorySettlement())
			{
				if (LordEncounterBehavior.IsEncounterRedirectSuspended() || LordEncounterRedirectGuard.IsSuppressed())
				{
					Logger.LogTrace("UI_Intercept", "Pending meeting victory settlement is active, but redirect is suspended/suppressed; keep native 'encounter' menu.");
					return true;
				}
				try
				{
					Hero hero = null;
					try
					{
						hero = PlayerEncounter.EncounteredParty?.LeaderHero;
					}
					catch
					{
						hero = null;
					}
					if (hero != null && hero != Hero.MainHero && hero.IsLord)
					{
						LordEncounterBehavior.SetTarget(hero);
					}
				}
				catch
				{
				}
				Logger.LogTrace("UI_Intercept", "Redirecting native 'encounter' to 'AnimusForge_lord_encounter' due to pending meeting victory settlement.");
				menuId = "AnimusForge_lord_encounter";
			}
			else if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("UI_Intercept", "Custom encounter menu is disabled for current encounter; keep native 'encounter' menu.");
			}
			else if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("UI_Intercept", "Encounter redirect is suspended; keep native 'encounter' menu.");
			}
			else
			{
				if (PlayerEncounter.Current == null || PlayerEncounterCompat.HasCampaignBattleResult() || LordEncounterRedirectGuard.IsSuppressed())
				{
					return true;
				}
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
					{
						return true;
					}
					MapEvent mapEvent = PlayerEncounterCompat.GetCurrentMapEventSafe();
					if (mapEvent != null)
					{
						Logger.LogTrace("UI_Intercept", "Active encounter battle context detected; keep native 'encounter' menu.");
						return true;
					}
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null || (encounteredParty.NumberOfAllMembers <= 0 && encounteredParty.NumberOfHealthyMembers <= 0))
				{
					return true;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (leaderHero != null && leaderHero != Hero.MainHero && leaderHero.IsLord)
				{
					ProactiveNpcRequestBehavior.MarkEncounterOpened(leaderHero);
					if (LordEncounterBehavior.IsVillageRaidEncounterContext(leaderHero))
					{
						Logger.LogTrace("UI_Intercept", $"Village raid context detected; keep native 'encounter' menu. Target={leaderHero.Name}");
						return true;
					}
					Logger.LogTrace("UI_Intercept", $"拦截到 'encounter' 菜单请求，重定向至 'AnimusForge_lord_encounter' (目标: {leaderHero.Name})");
					LordEncounterBehavior.SetTarget(leaderHero);
					if (PlayerEncounter.Current != null)
					{
						PlayerEncounter.LeaveEncounter = false;
						PlayerEncounter.Current.IsPlayerWaiting = false;
					}
					menuId = "AnimusForge_lord_encounter";
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("UI_Intercept", "[ERROR] " + ex.ToString());
		}
		return true;
	}
}
