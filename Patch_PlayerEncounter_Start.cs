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
			LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "postfix_enter");
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter attack is pending; skip custom encounter menu redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_pending_attack");
			}
			else if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Encounter redirect is suspended; skip custom encounter menu redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_redirect_suspended");
			}
			else if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Native hostile settlement request meeting detected; skip custom encounter menu redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_native_settlement_request_meeting");
			}
			else if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("Patch_PlayerEncounter_Start", "Custom encounter menu is disabled for current encounter; skip custom encounter menu redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_custom_disabled");
			}
			else
			{
				if (PlayerEncounter.Current == null)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_current_null");
					return;
				}
				if (PlayerEncounterCompat.HasCampaignBattleResult())
				{
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_campaign_result");
					return;
				}
				if (LordEncounterRedirectGuard.IsSuppressed())
				{
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_redirect_guard_suppressed");
					return;
				}
				if (PlayerEncounter.LeaveEncounter)
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter leave is pending; skip custom encounter menu redirect.");
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_leave_encounter");
					return;
				}
				if (PlayerEncounter.PlayerSurrender)
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native player surrender is pending; skip custom encounter menu redirect.");
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_player_surrender");
					return;
				}
				PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
				if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_state_" + encounterState);
					return;
				}
				try
				{
					if (PlayerEncounterCompat.HasResolvedEncounterBattleContext())
					{
						Logger.LogTrace("Patch_PlayerEncounter_Start", "Resolved encounter battle context detected; skip custom encounter menu redirect.");
						LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_resolved_map_event");
						return;
					}
				}
				catch
				{
				}
				if (LordEncounterBehavior.IsNativeEncounterActivityContext())
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter activity context detected; skip custom encounter menu redirect.");
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_native_activity_context");
					return;
				}
				if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Sea encounter context detected; keep native encounter flow.");
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_sea_context");
					return;
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_encountered_party_null");
					return;
				}
				if (LordEncounterBehavior.IsNativeEncounterActivityContext(encounteredParty.LeaderHero))
				{
					Logger.LogTrace("Patch_PlayerEncounter_Start", "Native encounter activity target detected; skip custom encounter menu redirect.");
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_native_activity_target", null, encounteredParty.LeaderHero, encounteredParty);
					return;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (!LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(leaderHero, encounteredParty))
				{
					if (leaderHero != null && leaderHero != Hero.MainHero && leaderHero.IsLord)
					{
						Logger.LogTrace("Patch_PlayerEncounter_Start", $"Encounter leader is not an eligible kingdom noble target; skip custom encounter menu redirect. Target={leaderHero.Name}, Party={encounteredParty.Name}");
					}
					LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "skip_ineligible_target", null, leaderHero, encounteredParty);
					return;
				}
				ProactiveNpcRequestBehavior.MarkEncounterOpened(leaderHero);
				LordEncounterBehavior.LogEncounterDiagnostic("PlayerEncounter.Start", "redirect_custom_lord_encounter", null, leaderHero, encounteredParty);
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
			Logger.LogImmediate("Logic", "[EncounterDiag] stage=PlayerEncounter.Start | reason=exception | error=" + ex);
		}
	}
}
