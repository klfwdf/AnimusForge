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
			bool logEncounterDiag = LordEncounterBehavior.ShouldLogEncounterDiagnosticForMenu(menuId);
			if (logEncounterDiag)
			{
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "prefix_enter", menuId);
			}
			if (SiegeAiInterventionBehavior.TryHandleDirectMassacreAftermathMenuForExternal(menuId, "GameMenu.ActivateGameMenu:" + (menuId ?? "N/A")))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation for direct GCCZ massacre aftermath. Menu=" + (menuId ?? "N/A"));
				if (logEncounterDiag)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "gccz_direct_massacre_suppressed", menuId);
				}
				return false;
			}
			if (SiegeAiInterventionBehavior.TryHandleDirectPlunderAftermathMenuForExternal(menuId, "GameMenu.ActivateGameMenu:" + (menuId ?? "N/A")))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation for direct GCCZ plunder aftermath. Menu=" + (menuId ?? "N/A"));
				if (logEncounterDiag)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "gccz_direct_plunder_suppressed", menuId);
				}
				return false;
			}
			if (SiegeAiInterventionBehavior.ShouldRedirectResolvedAftermathMenuForExternal(menuId))
			{
				Logger.LogTrace("UI_Intercept", "Skipping native siege aftermath menu after GCCZ resolution and finishing encounter. Menu=" + (menuId ?? "N/A"));
				SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuInitForExternal("GameMenu.ActivateGameMenu:" + (menuId ?? "N/A"));
				if (logEncounterDiag)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "gccz_resolved_after_suppressed", menuId);
				}
				return false;
			}
			if (SiegeAiInterventionBehavior.TryHandleNativeAftermathMenuActivationForExternal(menuId))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native siege aftermath menu activation after GCCZ intervention. Menu=" + (menuId ?? "N/A"));
				if (logEncounterDiag)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "gccz_native_after_suppressed", menuId);
				}
				return false;
			}
			if (menuId == "AnimusForge_lord_encounter" && LordEncounterBehavior.IsCustomEncounterMenuHardSuppressedForExternal())
			{
				Logger.LogTrace("UI_Intercept", "Custom encounter menu activation suppressed while meeting battle is returning to the world map.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "custom_menu_hard_suppressed", menuId);
				return false;
			}
			if (menuId == "AnimusForge_lord_encounter" && LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
			{
				Logger.LogTrace("UI_Intercept", "Custom encounter menu activation suppressed during native hostile settlement request meeting.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "native_settlement_request_suppressed_custom_menu", menuId);
				return false;
			}
			if (menuId == "AnimusForge_lord_encounter" && MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
			{
				Logger.LogTrace("UI_Intercept", "Custom encounter menu activation requested at sea; redirecting back to native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "custom_menu_at_sea_redirect_native", menuId);
				menuId = "encounter";
				return true;
			}
			if (!(menuId == "encounter"))
			{
				return true;
			}
			if (DuelBehavior.TrySuppressStaleWildernessDuelEncounterMenuActivation(menuId, "GameMenu.ActivateGameMenu"))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed stale native 'encounter' menu after AnimusForge wilderness duel cleanup.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "wilderness_duel_stale_encounter_suppressed", menuId);
				return false;
			}
			if (LordEncounterBehavior.TryResolvePendingPeacefulMeetingCleanupForExternal("activate_native_encounter_menu"))
			{
				Logger.LogTrace("UI_Intercept", "Suppressed native 'encounter' menu after peaceful custom meeting cleanup.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "peaceful_meeting_cleanup_suppressed_native_encounter", menuId);
				return false;
			}
			if (LordEncounterBehavior.HasPendingNativeEncounterAttackForExternal())
			{
				Logger.LogTrace("UI_Intercept", "Native encounter attack is pending; keep native 'encounter' menu and skip custom redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_pending_attack", menuId);
				return true;
			}
			if (LordEncounterBehavior.HasPendingMeetingBattleNativeResultForExternal())
			{
				Logger.LogTrace("UI_Intercept", "Meeting battle native result is pending; keep native 'encounter' menu and skip custom redirect.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_pending_meeting_result", menuId);
				return true;
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.LeaveEncounter)
			{
				Logger.LogTrace("UI_Intercept", "Native encounter leave is pending; keep native 'encounter' menu so PlayerEncounter.Finish can run.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_leave_encounter", menuId);
				return true;
			}
			if (PlayerEncounter.Current != null && PlayerEncounter.PlayerSurrender)
			{
				Logger.LogTrace("UI_Intercept", "Native player surrender is pending; keep native 'encounter' menu so surrender result can resolve.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_player_surrender", menuId);
				return true;
			}
			if (LordEncounterBehavior.IsNativeEncounterActivityContext())
			{
				Logger.LogTrace("UI_Intercept", "Native encounter activity context detected; keep native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_activity_context", menuId);
				return true;
			}
			if (LordEncounterBehavior.IsNativeSettlementRequestMeetingContext())
			{
				Logger.LogTrace("UI_Intercept", "Native hostile settlement request meeting detected; keep native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_settlement_request_meeting", menuId);
				return true;
			}
			if (MapSeaContextGuard.IsCurrentPlayerEncounterAtSea())
			{
				Logger.LogTrace("UI_Intercept", "Sea encounter context detected; keep native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_sea_context", menuId);
				return true;
			}
			if (LordEncounterBehavior.IsCustomEncounterMenuDisabledForCurrentEncounter())
			{
				Logger.LogTrace("UI_Intercept", "Custom encounter menu is disabled for current encounter; keep native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_custom_disabled", menuId);
			}
			else if (LordEncounterBehavior.IsEncounterRedirectSuspended())
			{
				Logger.LogTrace("UI_Intercept", "Encounter redirect is suspended; keep native 'encounter' menu.");
				LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_redirect_suspended", menuId);
			}
			else
			{
				if (PlayerEncounter.Current == null)
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_current_null", menuId);
					return true;
				}
				if (PlayerEncounterCompat.HasCampaignBattleResult())
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_campaign_result", menuId);
					return true;
				}
				if (LordEncounterRedirectGuard.IsSuppressed())
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_redirect_guard_suppressed", menuId);
					return true;
				}
				if (PlayerEncounter.Current != null)
				{
					PlayerEncounterState encounterState = PlayerEncounter.Current.EncounterState;
					if (encounterState != PlayerEncounterState.Begin && encounterState != PlayerEncounterState.Wait)
					{
						LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_state_" + encounterState, menuId);
						return true;
					}
					MapEvent mapEvent = PlayerEncounterCompat.GetCurrentMapEventSafe();
					if (PlayerEncounterCompat.IsResolvedMapEvent(mapEvent))
					{
						Logger.LogTrace("UI_Intercept", "Resolved encounter battle context detected; keep native 'encounter' menu.");
						LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_resolved_map_event", menuId);
						return true;
					}
				}
				PartyBase encounteredParty = PlayerEncounter.EncounteredParty;
				if (encounteredParty == null || (encounteredParty.NumberOfAllMembers <= 0 && encounteredParty.NumberOfHealthyMembers <= 0))
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_no_or_empty_encountered_party", menuId, null, encounteredParty);
					return true;
				}
				Hero leaderHero = encounteredParty.LeaderHero;
				if (LordEncounterBehavior.IsEligibleCustomLordEncounterTarget(leaderHero, encounteredParty))
				{
					ProactiveNpcRequestBehavior.MarkEncounterOpened(leaderHero);
					if (LordEncounterBehavior.IsNativeEncounterActivityContext(leaderHero))
					{
						Logger.LogTrace("UI_Intercept", $"Native encounter activity target detected; keep native 'encounter' menu. Target={leaderHero.Name}");
						LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_activity_target_after_eligible", menuId, leaderHero, encounteredParty);
						return true;
					}
					Logger.LogTrace("UI_Intercept", $"拦截到 'encounter' 菜单请求，重定向至 'AnimusForge_lord_encounter' (目标: {leaderHero.Name})");
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "redirect_custom_lord_encounter", menuId, leaderHero, encounteredParty);
					LordEncounterBehavior.SetTarget(leaderHero);
					if (PlayerEncounter.Current != null)
					{
						PlayerEncounter.LeaveEncounter = false;
						PlayerEncounter.Current.IsPlayerWaiting = false;
					}
					menuId = "AnimusForge_lord_encounter";
				}
				else if (leaderHero != null && leaderHero != Hero.MainHero && leaderHero.IsLord)
				{
					Logger.LogTrace("UI_Intercept", $"Encounter leader is a lord-shaped non-kingdom-noble target; keep native 'encounter' menu. Target={leaderHero.Name}, Party={encounteredParty.Name}");
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_ineligible_lord_target", menuId, leaderHero, encounteredParty);
				}
				else
				{
					LordEncounterBehavior.LogEncounterDiagnostic("GameMenu.ActivateGameMenu", "keep_native_no_eligible_target", menuId, leaderHero, encounteredParty);
				}
			}
		}
		catch (Exception ex)
		{
			Logger.LogTrace("UI_Intercept", "[ERROR] " + ex.ToString());
			Logger.LogImmediate("Logic", "[EncounterDiag] stage=GameMenu.ActivateGameMenu | reason=exception | error=" + ex);
		}
		return true;
	}
}
