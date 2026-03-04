using System;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

[HarmonyPatch(typeof(PlayerEncounter), "Start")]
public static class Patch_PlayerEncounter_Start
{
	public static void Postfix()
	{
		try
		{
			if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Encounter redirect is suspended; skip custom encounter menu redirect.");
			}
			else if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Custom encounter menu is disabled for current encounter; skip custom encounter menu redirect.");
			}
			else
			{
				if (PlayerEncounter.Current == null || PlayerEncounter.CampaignBattleResult != null || LordEncounterRedirectGuard.IsSuppressed())
				{
					return;
				}
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
				{
					return;
				}
				try
				{
					if (PlayerEncounter.Battle != null || PlayerEncounter.EncounteredBattle != null)
					{
						return;
					}
				}
				catch
				{
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null)
				{
					return;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (leaderHero == null || leaderHero == Hero.MainHero || !leaderHero.IsLord)
				{
					return;
				}
				Logger.Log("Patch_PlayerEncounter_Start", $"检测到领主遭遇: {leaderHero.Name}，强制重定向到 AnimusForge_lord_encounter");
				try
				{
					if (Campaign.Current?.ConversationManager != null)
					{
						Campaign.Current.ConversationManager.EndConversation();
					}
				}
				catch
				{
				}
				LordEncounterBehavior.OpenEncounterMenu(leaderHero);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Patch_PlayerEncounter_Start", "[ERROR] " + ex.ToString());
		}
	}
}
