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
	public static void Prefix(ref string menuId)
	{
		try
		{
			if (!(menuId == "encounter"))
			{
				return;
			}
			if (LordEncounterBehavior.HasPendingMeetingBattleVictorySettlement())
			{
				if (LordEncounterBehavior.IsEncounterRedirectSuspended() || LordEncounterRedirectGuard.IsSuppressed())
				{
					Logger.LogTrace("UI_Intercept", "Pending meeting victory settlement is active, but redirect is suspended/suppressed; keep native 'encounter' menu.");
					return;
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
				if (PlayerEncounter.Current == null || PlayerEncounter.CampaignBattleResult != null || LordEncounterRedirectGuard.IsSuppressed())
				{
					return;
				}
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
					{
						return;
					}
					MapEvent mapEvent = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
					if (mapEvent != null && (mapEvent.HasWinner || mapEvent.IsFinalized))
					{
						return;
					}
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null || (encounteredParty.NumberOfAllMembers <= 0 && encounteredParty.NumberOfHealthyMembers <= 0))
				{
					return;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (leaderHero != null && leaderHero != Hero.MainHero && leaderHero.IsLord)
				{
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
	}
}
