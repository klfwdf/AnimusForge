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
		//IL_0135: Unknown result type (might be due to invalid IL or missing references)
		//IL_013a: Unknown result type (might be due to invalid IL or missing references)
		//IL_013c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0140: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Invalid comparison between Unknown and I4
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
					Hero val = null;
					try
					{
						PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
						val = ((encounteredParty != null) ? encounteredParty.LeaderHero : null);
					}
					catch
					{
						val = null;
					}
					if (val != null && val != Hero.MainHero && val.IsLord)
					{
						LordEncounterBehavior.SetTarget(val);
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
					if ((int)encounterState != 0 && (int)encounterState != 1)
					{
						return;
					}
					MapEvent val2 = PlayerEncounter.Battle ?? PlayerEncounter.EncounteredBattle;
					if (val2 != null && (val2.HasWinner || val2.IsFinalized))
					{
						return;
					}
				}
				PartyBase encounteredParty2 = PlayerEncounter.EncounteredParty;
				if (encounteredParty2 == null || (encounteredParty2.NumberOfAllMembers <= 0 && encounteredParty2.NumberOfHealthyMembers <= 0))
				{
					return;
				}
				Hero leaderHero = encounteredParty2.LeaderHero;
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
