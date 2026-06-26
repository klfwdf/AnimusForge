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
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter attack is pending; skip custom encounter menu redirect.");
			}
			else if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Encounter redirect is suspended; skip custom encounter menu redirect.");
			}
			else if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Custom encounter menu is disabled for current encounter; skip custom encounter menu redirect.");
			}
			else
			{
				if (PlayerEncounter.Current == null || PlayerEncounterCompat.HasCampaignBattleResult() || LordEncounterRedirectGuard.IsSuppressed())
				{
					return;
				}
				if (PlayerEncounter.LeaveEncounter)
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter leave is pending; skip custom encounter menu redirect.");
					return;
				}
				if (PlayerEncounter.PlayerSurrender)
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native player surrender is pending; skip custom encounter menu redirect.");
					return;
				}
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
				{
					return;
				}
				try
				{
					if (PlayerEncounterCompat.HasEncounterBattleContext())
					{
						Logger.LogTrace("Patch_PlayerEncounter_Start", "Active encounter battle context detected; skip custom encounter menu redirect.");
						return;
					}
				}
				catch
				{
				}
				if (LordEncounterBehavior.IsNativeEncounterActivityContext())
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter activity context detected; skip custom encounter menu redirect.");
					return;
				}
				if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Sea encounter context detected; keep native encounter flow.");
					return;
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null)
				{
					return;
				}
				if (LordEncounterBehavior.IsNativeEncounterActivityContext(encounteredParty.LeaderHero))
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter activity target detected; skip custom encounter menu redirect.");
					return;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (leaderHero == null || leaderHero == Hero.MainHero || !leaderHero.IsLord)
				{
					return;
				}
				ProactiveNpcRequestBehavior.MarkEncounterOpened(leaderHero);
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
