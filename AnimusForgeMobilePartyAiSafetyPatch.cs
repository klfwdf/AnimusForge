using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class AnimusForgeMobilePartyAiSafetyPatch
{
	private const string LogSource = "MobilePartyAiSafety";
	private const int MaxLoggedKeys = 128;
	private const int MaxFactionSettlementsChecked = 256;

	private static readonly HashSet<string> LoggedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
	private static bool _patched;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			PatchPartyHourlyAiTick(harmony);
			PatchCampaignDispatcherAiHourlyTick(harmony);
			PatchAiVisitSettlementTick(harmony);
			PatchPartyWageModel(harmony);
			PatchPartyUpgrader(harmony);
			PatchRecruitment(harmony);
			PatchPartyDiplomaticHandler(harmony);
			PatchChangeShipOwnerAction(harmony);
			PatchNavalShipTradeOwnerChanged(harmony);
			PatchNavalBanditSafeZone(harmony);
		}
		catch (Exception ex)
		{
			Logger.Log(LogSource, "Failed to apply mobile party AI guards: " + ex.Message);
		}
	}

	public static bool PartyHourlyAiTickPrefix(object[] __args)
	{
		try
		{
			MobileParty party = ExtractParty(__args);
			if (CampaignTickDiagnosticsPatch.ConsumePriorCrashSuspectPartySkip(party, out string priorCrashReason))
			{
				TryDelayNativeAiDecisionOneTick(party, priorCrashReason);
				LogGuard("party_hourly_ai_prior_crash_skip", party, priorCrashReason);
				return false;
			}
			if (!ShouldSkipNativeAiForUtilityParty(party, out string reason))
			{
				if (!IsUnsafeForNativeHourlyAiParty(party, out reason))
				{
					return true;
				}
				TryDelayNativeAiDecisionOneTick(party, reason);
				LogGuard("party_hourly_ai_unsafe_skip", party, reason);
				return false;
			}
			TryLockNativeAiDecisions(party, reason);
			LogGuard("party_hourly_ai_skip", party, reason);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static Exception PartyHourlyAiTickFinalizer(Exception __exception, object[] __args, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			MobileParty party = ExtractParty(__args);
			if (ShouldSuppressNativeHourlyAiException(party, __exception, out string reason))
			{
				TryDelayNativeAiDecisionOneTick(party, reason);
				LogGuard("party_hourly_ai_exception_suppressed", party, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static Exception CampaignDispatcherAiHourlyTickFinalizer(Exception __exception, object[] __args, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			MobileParty party = ExtractParty(__args);
			if (ShouldSuppressNativeHourlyAiException(party, __exception, out string reason))
			{
				TryDelayNativeAiDecisionOneTick(party, reason);
				LogGuard("dispatcher_ai_hourly_exception_suppressed", party, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static bool AiVisitSettlementPrefix(object[] __args)
	{
		try
		{
			MobileParty party = ExtractParty(__args);
			if (ShouldSkipNativeAiForUtilityParty(party, out string utilityReason))
			{
				TryLockNativeAiDecisions(party, utilityReason);
				LogGuard("visit_settlement_skip", party, utilityReason);
				return false;
			}
			if (IsUnsafeForNativeAiVisitSettlement(party, out string unsafeReason))
			{
				LogGuard("visit_settlement_unsafe_skip", party, unsafeReason);
				return false;
			}
			return true;
		}
		catch
		{
			return true;
		}
	}

	public static Exception AiVisitSettlementFinalizer(Exception __exception, object[] __args, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			MobileParty party = ExtractParty(__args);
			if (ShouldSuppressNativeHourlyAiException(party, __exception, out string utilityReason))
			{
				LogGuard("visit_settlement_exception_suppressed", party, utilityReason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static bool GetTotalWagePrefix(MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions, ref ExplainedNumber __result)
	{
		try
		{
			if (!ShouldUseSafeWageFallback(mobileParty, troopRoster, out string reason, out bool zeroWage))
			{
				return true;
			}
			__result = BuildSafeWageFallback(troopRoster, includeDescriptions, zeroWage);
			LogGuard("wage_safe_fallback", mobileParty, reason);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static Exception GetTotalWageFinalizer(Exception __exception, MobileParty mobileParty, TroopRoster troopRoster, bool includeDescriptions, ref ExplainedNumber __result, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (IsRecoverableNativeAiStateException(__exception) && ShouldUseSafeWageFallback(mobileParty, troopRoster, out string reason, out bool zeroWage))
			{
				__result = BuildSafeWageFallback(troopRoster, includeDescriptions, zeroWage);
				LogGuard("wage_exception_suppressed", mobileParty, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static bool UpgradeReadyTroopsPrefix(PartyBase party)
	{
		try
		{
			if (!ShouldSkipNativeUpgradeForPartyBase(party, out string reason, out MobileParty mobileParty))
			{
				return true;
			}
			LogGuard("upgrade_ready_troops_skip", mobileParty, reason);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static Exception UpgradeReadyTroopsFinalizer(Exception __exception, PartyBase party, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (IsRecoverableNativeAiStateException(__exception) && ShouldSkipNativeUpgradeForPartyBase(party, out string reason, out MobileParty mobileParty))
			{
				LogGuard("upgrade_ready_troops_exception_suppressed", mobileParty, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static bool CheckRecruitingPrefix(MobileParty mobileParty, Settlement settlement)
	{
		try
		{
			if (!ShouldSkipNativeRecruiting(mobileParty, settlement, out string reason))
			{
				return true;
			}
			TryLockNativeAiDecisions(mobileParty, reason);
			LogGuard("recruiting_skip", mobileParty, reason);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static Exception CheckRecruitingFinalizer(Exception __exception, MobileParty mobileParty, Settlement settlement, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (IsRecoverableNativeAiStateException(__exception) && ShouldSkipNativeRecruiting(mobileParty, settlement, out string reason))
			{
				LogGuard("recruiting_exception_suppressed", mobileParty, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static Exception CheckSettlementSuitabilityFinalizer(Exception __exception, IEnumerable<MobileParty> parties, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (IsRecoverableNativeAiStateException(__exception) && ContainsUnsafePartyForNativeSettlementSuitability(parties, out string reason, out MobileParty party))
			{
				LogGuard("settlement_suitability_exception_suppressed", party, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static bool ChangeShipOwnerPrefix(PartyBase newOwner, Ship ship)
	{
		try
		{
			if (!ShouldSkipShipOwnerChange(newOwner, ship, out string reason, out MobileParty mobileParty))
			{
				return true;
			}
			LogGuard("ship_owner_change_skip", mobileParty, reason);
			return false;
		}
		catch
		{
			return true;
		}
	}

	public static Exception ChangeShipOwnerFinalizer(Exception __exception, PartyBase newOwner, Ship ship, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (IsRecoverableNativeAiStateException(__exception) && ShouldSuppressShipOwnerChangeException(newOwner, ship, out string reason, out MobileParty mobileParty))
			{
				LogGuard("ship_owner_change_exception_suppressed", mobileParty, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
	}

	public static Exception NavalShipTradeOwnerChangedFinalizer(Exception __exception, Ship ship, PartyBase oldOwner, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		try
		{
			if (!IsRecoverableNativeAiStateException(__exception))
			{
				return __exception;
			}
			PartyBase currentOwner = GetShipOwnerSafe(ship);
			MobileParty party = ExtractMobileParty(currentOwner) ?? ExtractMobileParty(oldOwner);
			string reason = "naval_ship_trade_owner_changed:" + DescribeShip(ship);
			LogGuard("naval_ship_trade_exception_suppressed", party, reason, __exception, __originalMethod);
			return null;
		}
		catch
		{
		}
		return __exception;
	}

	public static Exception NavalBanditSafeZoneFinalizer(Exception __exception, ref bool __result, MethodBase __originalMethod)
	{
		if (__exception == null)
		{
			return null;
		}
		if (!(__exception is NullReferenceException))
		{
			return __exception;
		}
		__result = false;
		LogGuard(
			"naval_safe_zone_null_suppressed",
			null,
			"NavalDLC closest entrance or map-distance state unavailable",
			__exception,
			__originalMethod);
		return null;
	}

	private static void PatchPartyHourlyAiTick(Harmony harmony)
	{
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiPartyThinkBehavior");
		MethodInfo target = type == null ? null : AccessTools.Method(type, "PartyHourlyAiTick", new[] { typeof(MobileParty) });
		if (target == null)
		{
			Logger.Log(LogSource, "AiPartyThinkBehavior.PartyHourlyAiTick not found; utility party AI guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(PartyHourlyAiTickPrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(PartyHourlyAiTickFinalizer)));
		Logger.Log(LogSource, "AiPartyThinkBehavior.PartyHourlyAiTick utility party guard applied.");
	}

	private static void PatchCampaignDispatcherAiHourlyTick(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(CampaignEventDispatcher), "AiHourlyTick", new[] { typeof(MobileParty), typeof(PartyThinkParams) });
		if (target == null)
		{
			Logger.Log(LogSource, "CampaignEventDispatcher.AiHourlyTick not found; dispatcher AI guard skipped.");
			return;
		}
		harmony.Patch(target, finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(CampaignDispatcherAiHourlyTickFinalizer)));
		Logger.Log(LogSource, "CampaignEventDispatcher.AiHourlyTick guard applied.");
	}

	private static void PatchAiVisitSettlementTick(Harmony harmony)
	{
		Type type = AccessTools.TypeByName("TaleWorlds.CampaignSystem.CampaignBehaviors.AiBehaviors.AiVisitSettlementBehavior");
		MethodInfo target = type == null ? null : AccessTools.Method(type, "AiHourlyTick", new[] { typeof(MobileParty), typeof(PartyThinkParams) });
		if (target == null)
		{
			Logger.Log(LogSource, "AiVisitSettlementBehavior.AiHourlyTick not found; visit settlement guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(AiVisitSettlementPrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(AiVisitSettlementFinalizer)));
		Logger.Log(LogSource, "AiVisitSettlementBehavior.AiHourlyTick guard applied.");
	}

	private static void PatchPartyWageModel(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(DefaultPartyWageModel), nameof(DefaultPartyWageModel.GetTotalWage), new[] { typeof(MobileParty), typeof(TroopRoster), typeof(bool) });
		if (target == null)
		{
			Logger.Log(LogSource, "DefaultPartyWageModel.GetTotalWage not found; wage guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(GetTotalWagePrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(GetTotalWageFinalizer)));
		Logger.Log(LogSource, "DefaultPartyWageModel.GetTotalWage guard applied.");
	}

	private static void PatchPartyUpgrader(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(PartyUpgraderCampaignBehavior), nameof(PartyUpgraderCampaignBehavior.UpgradeReadyTroops), new[] { typeof(PartyBase) });
		if (target == null)
		{
			Logger.Log(LogSource, "PartyUpgraderCampaignBehavior.UpgradeReadyTroops not found; upgrade guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(UpgradeReadyTroopsPrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(UpgradeReadyTroopsFinalizer)));
		Logger.Log(LogSource, "PartyUpgraderCampaignBehavior.UpgradeReadyTroops guard applied.");
	}

	private static void PatchRecruitment(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(RecruitmentCampaignBehavior), "CheckRecruiting", new[] { typeof(MobileParty), typeof(Settlement) });
		if (target == null)
		{
			Logger.Log(LogSource, "RecruitmentCampaignBehavior.CheckRecruiting not found; recruitment guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(CheckRecruitingPrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(CheckRecruitingFinalizer)));
		Logger.Log(LogSource, "RecruitmentCampaignBehavior.CheckRecruiting guard applied.");
	}

	private static void PatchPartyDiplomaticHandler(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(PartyDiplomaticHandlerCampaignBehavior), "CheckSettlementSuitabilityForParties", new[] { typeof(IEnumerable<MobileParty>) });
		if (target == null)
		{
			Logger.Log(LogSource, "PartyDiplomaticHandlerCampaignBehavior.CheckSettlementSuitabilityForParties not found; diplomatic guard skipped.");
			return;
		}
		harmony.Patch(target, finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(CheckSettlementSuitabilityFinalizer)));
		Logger.Log(LogSource, "PartyDiplomaticHandlerCampaignBehavior.CheckSettlementSuitabilityForParties guard applied.");
	}

	private static void PatchChangeShipOwnerAction(Harmony harmony)
	{
		MethodInfo target = AccessTools.Method(typeof(ChangeShipOwnerAction), "ApplyInternal");
		if (target == null)
		{
			Logger.Log(LogSource, "ChangeShipOwnerAction.ApplyInternal not found; ship owner guard skipped.");
			return;
		}
		harmony.Patch(
			target,
			prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(ChangeShipOwnerPrefix)),
			finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(ChangeShipOwnerFinalizer)));
		Logger.Log(LogSource, "ChangeShipOwnerAction.ApplyInternal guard applied.");
	}

	private static void PatchNavalShipTradeOwnerChanged(Harmony harmony)
	{
		Type type = AccessTools.TypeByName("NavalDLC.CampaignBehaviors.ShipTradeCampaignBehavior");
		MethodInfo target = type == null ? null : AccessTools.Method(type, "OnShipOwnerChanged");
		if (target == null)
		{
			Logger.Log(LogSource, "NavalDLC ShipTradeCampaignBehavior.OnShipOwnerChanged not found; naval ship trade guard skipped.");
			return;
		}
		harmony.Patch(target, finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(NavalShipTradeOwnerChangedFinalizer)));
		Logger.Log(LogSource, "NavalDLC ShipTradeCampaignBehavior.OnShipOwnerChanged guard applied.");
	}

	private static void PatchNavalBanditSafeZone(Harmony harmony)
	{
		Type type = AccessTools.TypeByName("NavalDLC.GameComponents.NavalDLCBanditDensityModel");
		MethodInfo target = type == null ? null : AccessTools.Method(type, "IsPositionInsideNavalSafeZone");
		if (target == null)
		{
			Logger.Log(LogSource, "NavalDLC NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone not found; naval safe-zone guard skipped.");
			return;
		}
		harmony.Patch(target, finalizer: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(NavalBanditSafeZoneFinalizer)));
		Logger.Log(LogSource, "NavalDLC NavalDLCBanditDensityModel.IsPositionInsideNavalSafeZone guard applied.");
	}

	private static MobileParty ExtractParty(object[] args)
	{
		if (args == null || args.Length == 0)
		{
			return null;
		}
		return args[0] as MobileParty;
	}

	private static bool ShouldSkipNativeAiForUtilityParty(MobileParty party, out string reason)
	{
		reason = "";
		if (party == null)
		{
			return false;
		}
		try
		{
			if (CourierDeliveryBehavior.IsCourierParty(party))
			{
				reason = "courier";
				return true;
			}
		}
		catch (Exception ex)
		{
			reason = "courier_check_exception:" + ex.GetType().Name;
			return true;
		}
		try
		{
			if (NobleGatheringBehavior.IsTemporaryGatheringParty(party))
			{
				reason = "noble_gathering_temp";
				return true;
			}
		}
		catch (Exception ex)
		{
			reason = "noble_gathering_check_exception:" + ex.GetType().Name;
			return true;
		}
		if (IsAnimusForgeUtilityPartyComponent(party, out reason))
		{
			return true;
		}
		string id = party.StringId ?? "";
		if (StartsWithAny(id,
			"af_courier_",
			"af_noble_gathering_temp_",
			"animusforge_wilderness_duel_",
			"animusforge_military_exercise_opponent_",
			"animusforge_military_exercise_holding_",
			"animusforge_troop_inspection_dummy_",
			"animusforge_troop_inspection_selection_pool_",
			"animusforge_troop_inspection_holding_"))
		{
			reason = "animusforge_utility_id";
			return true;
		}
		return false;
	}

	private static bool ShouldUseSafeWageFallback(MobileParty party, TroopRoster roster, out string reason, out bool zeroWage)
	{
		reason = "";
		zeroWage = false;
		try
		{
			if (ShouldSkipNativeAiForUtilityParty(party, out reason))
			{
				zeroWage = true;
				return true;
			}
			if (party == null)
			{
				reason = "wage_party_null";
				return true;
			}
			if (party == MobileParty.MainParty)
			{
				return false;
			}
			if (roster == null)
			{
				reason = "wage_roster_null";
				return true;
			}
			if (!ValidateMobilePartyForNativeDailySystems(party, "wage_party", out reason))
			{
				return true;
			}
			if (!ValidateRosterForNativeWage(roster, "wage_roster", out reason))
			{
				return true;
			}
			Settlement currentSettlement = party.CurrentSettlement;
			if (party.IsGarrison && currentSettlement?.Town != null)
			{
				if (currentSettlement.Owner == null || currentSettlement.Owner.Culture == null)
				{
					reason = "wage_garrison_owner_culture_null";
					return true;
				}
			}
			Hero leader = party.LeaderHero;
			if (leader != null && leader.Clan == null)
			{
				reason = "wage_leader_clan_null";
				return true;
			}
			if (party.EffectiveQuartermaster != null && party.EffectiveQuartermaster.CharacterObject == null)
			{
				reason = "wage_quartermaster_character_null";
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "wage_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool IsAnimusForgeUtilityPartyComponent(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			string componentName = party?.PartyComponent?.GetType().FullName ?? "";
			if (string.IsNullOrWhiteSpace(componentName) || componentName.IndexOf("AnimusForge.", StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
			if (componentName.IndexOf("DummyPartyComponent", StringComparison.OrdinalIgnoreCase) >= 0
				|| componentName.IndexOf("TemporaryPartyComponent", StringComparison.OrdinalIgnoreCase) >= 0
				|| componentName.IndexOf("HoldingDummyPartyComponent", StringComparison.OrdinalIgnoreCase) >= 0)
			{
				reason = "animusforge_utility_component:" + componentName;
				return true;
			}
		}
		catch (Exception ex)
		{
			reason = "animusforge_component_check_exception:" + ex.GetType().Name;
			return true;
		}
		return false;
	}

	private static bool IsUnsafeForNativeHourlyAiParty(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			if (party == null)
			{
				reason = "hourly_ai_party_null";
				return true;
			}
			if (party == MobileParty.MainParty)
			{
				return false;
			}
			if (!ValidateMobilePartyForNativeDailySystems(party, "hourly_ai_party", out reason))
			{
				return true;
			}
			if (party.Ai == null)
			{
				reason = "hourly_ai_ai_null";
				return true;
			}
			IFaction mapFaction = party.MapFaction;
			if (party.IsBandit)
			{
				return false;
			}
			if (WillNativeAiVisitSettlementReturnBeforeRiskyReads(party, mapFaction))
			{
				return false;
			}
			if (party.Party.Owner == null)
			{
				reason = "hourly_ai_party_owner_null";
				return true;
			}
			if (party.Army != null && !ValidateArmyForNativeVisit(party, out reason))
			{
				reason = "hourly_ai_" + reason;
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "hourly_ai_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool ShouldSuppressNativeHourlyAiException(MobileParty party, Exception exception, out string reason)
	{
		reason = "";
		try
		{
			if (!IsRecoverableNativeAiStateException(exception))
			{
				return false;
			}
			if (party == MobileParty.MainParty)
			{
				return false;
			}
			if (ShouldSkipNativeAiForUtilityParty(party, out reason))
			{
				reason = "utility:" + reason;
				return true;
			}
			if (IsUnsafeForNativeHourlyAiParty(party, out reason))
			{
				reason = "unsafe_hourly_ai:" + reason;
				return true;
			}
			if (IsUnsafeForNativeAiVisitSettlement(party, out reason))
			{
				reason = "unsafe_visit_settlement:" + reason;
				return true;
			}
			if (IsKnownNativeHourlyAiStateException(exception))
			{
				reason = "known_native_hourly_ai_state_exception:" + exception.GetType().Name;
				return true;
			}
		}
		catch (Exception ex)
		{
			reason = "suppress_guard_exception:" + ex.GetType().Name;
			return true;
		}
		return false;
	}

	private static bool IsKnownNativeHourlyAiStateException(Exception exception)
	{
		string text = exception?.ToString() ?? "";
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		return text.IndexOf("AiVisitSettlementBehavior.AiHourlyTick", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("HeroHelper.StartRecruitingMoneyLimitForClanLeader", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("DefaultPartyWageModel.GetTotalWage", StringComparison.OrdinalIgnoreCase) >= 0
			|| text.IndexOf("PartyBaseHelper.HasFeat", StringComparison.OrdinalIgnoreCase) >= 0;
	}

	private static ExplainedNumber BuildSafeWageFallback(TroopRoster roster, bool includeDescriptions, bool zeroWage)
	{
		int total = 0;
		if (!zeroWage)
		{
			try
			{
				for (int i = 0; roster != null && i < roster.Count; i++)
				{
					TroopRosterElement element = roster.GetElementCopyAtIndex(i);
					CharacterObject character = element.Character;
					if (character == null)
					{
						continue;
					}
					int count = Math.Max(0, element.Number);
					int wage = Math.Max(0, character.TroopWage);
					if (count > 0 && wage > 0)
					{
						total += count * wage;
					}
				}
			}
			catch
			{
				total = 0;
			}
		}
		ExplainedNumber result = new ExplainedNumber(total, includeDescriptions);
		result.LimitMin(0f);
		return result;
	}

	private static bool ShouldSkipNativeUpgradeForPartyBase(PartyBase partyBase, out string reason, out MobileParty mobileParty)
	{
		reason = "";
		mobileParty = ExtractMobileParty(partyBase);
		try
		{
			if (partyBase == null)
			{
				reason = "upgrade_partybase_null";
				return true;
			}
			if (partyBase == PartyBase.MainParty)
			{
				return false;
			}
			if (mobileParty != null && ShouldSkipNativeAiForUtilityParty(mobileParty, out reason))
			{
				return true;
			}
			if (!partyBase.IsActive)
			{
				return false;
			}
			if (Campaign.Current == null || Campaign.Current.Models == null || Campaign.Current.Models.PartyTroopUpgradeModel == null || Campaign.Current.Models.PartyWageModel == null)
			{
				reason = "upgrade_campaign_models_unavailable";
				return true;
			}
			if (mobileParty == null)
			{
				reason = "upgrade_mobile_party_null";
				return true;
			}
			if (!ValidateMobilePartyForNativeDailySystems(mobileParty, "upgrade_party", out reason))
			{
				return true;
			}
			if (partyBase.Culture == null)
			{
				reason = "upgrade_party_culture_null";
				return true;
			}
			if (!ValidateRosterForNativeUpgrade(partyBase.MemberRoster, "upgrade_roster", out reason))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "upgrade_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool ShouldSkipNativeRecruiting(MobileParty party, Settlement settlement, out string reason)
	{
		reason = "";
		try
		{
			if (ShouldSkipNativeAiForUtilityParty(party, out reason))
			{
				return true;
			}
			if (party == null)
			{
				reason = "recruit_party_null";
				return true;
			}
			if (party == MobileParty.MainParty)
			{
				return false;
			}
			if (settlement == null)
			{
				reason = "recruit_settlement_null";
				return true;
			}
			if (Campaign.Current == null || Campaign.Current.Models == null || Campaign.Current.Models.PartyWageModel == null || Campaign.Current.Models.VolunteerModel == null)
			{
				reason = "recruit_campaign_models_unavailable";
				return true;
			}
			if (!ValidateMobilePartyForNativeDailySystems(party, "recruit_party", out reason))
			{
				return true;
			}
			if (!ValidateNativeVisitSettlementCandidate(settlement, "recruit_settlement", out reason))
			{
				return true;
			}
			if (settlement.IsTown && settlement.Town == null)
			{
				reason = "recruit_town_null";
				return true;
			}
			if (settlement.Notables == null)
			{
				reason = "recruit_notables_null";
				return true;
			}
			foreach (Hero notable in settlement.Notables)
			{
				if (notable == null)
				{
					reason = "recruit_notable_null";
					return true;
				}
				if (notable.VolunteerTypes == null || notable.VolunteerTypes.Length < 6)
				{
					reason = "recruit_volunteers_invalid";
					return true;
				}
			}
			Hero leader = party.LeaderHero;
			if (party.IsLordParty && (leader == null || leader.Clan == null))
			{
				reason = "recruit_lord_leader_clan_null";
				return true;
			}
			if (party.Party.PartySizeLimit <= 0)
			{
				reason = "recruit_party_size_limit_nonpositive";
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "recruit_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool ContainsUnsafePartyForNativeSettlementSuitability(IEnumerable<MobileParty> parties, out string reason, out MobileParty unsafeParty)
	{
		reason = "";
		unsafeParty = null;
		try
		{
			if (parties == null)
			{
				reason = "settlement_suitability_parties_null";
				return true;
			}
			int checkedCount = 0;
			foreach (MobileParty party in parties)
			{
				if (checkedCount++ >= MaxFactionSettlementsChecked)
				{
					break;
				}
				if (IsUnsafeForNativeSettlementSuitability(party, out reason))
				{
					unsafeParty = party;
					return true;
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "settlement_suitability_enumeration_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool IsUnsafeForNativeSettlementSuitability(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			if (ShouldSkipNativeAiForUtilityParty(party, out reason))
			{
				return true;
			}
			if (party == null)
			{
				reason = "settlement_suitability_party_null";
				return true;
			}
			Settlement currentSettlement = party.CurrentSettlement;
			if (currentSettlement == null)
			{
				return false;
			}
			if (party.MapFaction == null)
			{
				reason = "settlement_suitability_map_faction_null";
				return true;
			}
			if (currentSettlement.MapFaction == null)
			{
				reason = "settlement_suitability_settlement_faction_null";
				return true;
			}
			if (party.Army != null)
			{
				if (party.Army.LeaderParty == null)
				{
					reason = "settlement_suitability_army_leader_null";
					return true;
				}
				if (party.Army.Parties == null)
				{
					reason = "settlement_suitability_army_parties_null";
					return true;
				}
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "settlement_suitability_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool ShouldSkipShipOwnerChange(PartyBase newOwner, Ship ship, out string reason, out MobileParty mobileParty)
	{
		reason = "";
		mobileParty = ExtractMobileParty(newOwner);
		try
		{
			if (ship == null)
			{
				reason = "ship_null";
				return true;
			}
			if (ship.ShipHull == null)
			{
				reason = "ship_hull_null";
				return true;
			}
			if (newOwner == null)
			{
				reason = "ship_new_owner_null";
				return true;
			}
			if (mobileParty != null && ShouldSkipNativeAiForUtilityParty(mobileParty, out string utilityReason) && !ValidateMobilePartyForNativeDailySystems(mobileParty, "ship_owner_utility", out reason))
			{
				reason = utilityReason + ":" + reason;
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "ship_owner_change_guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool ShouldSuppressShipOwnerChangeException(PartyBase newOwner, Ship ship, out string reason, out MobileParty mobileParty)
	{
		if (ShouldSkipShipOwnerChange(newOwner, ship, out reason, out mobileParty))
		{
			return true;
		}
		try
		{
			PartyBase currentOwner = GetShipOwnerSafe(ship);
			MobileParty currentOwnerParty = ExtractMobileParty(currentOwner);
			if (currentOwnerParty != null && ShouldSkipNativeAiForUtilityParty(currentOwnerParty, out reason))
			{
				mobileParty = currentOwnerParty;
				return true;
			}
			mobileParty = ExtractMobileParty(newOwner);
			if (mobileParty != null && ShouldSkipNativeAiForUtilityParty(mobileParty, out reason))
			{
				return true;
			}
			if (ship == null || ship.ShipHull == null)
			{
				reason = "ship_invalid_after_exception";
				return true;
			}
		}
		catch (Exception ex)
		{
			reason = "ship_owner_exception_guard_exception:" + ex.GetType().Name;
			return true;
		}
		return false;
	}

	private static bool ValidateMobilePartyForNativeDailySystems(MobileParty party, string label, out string reason)
	{
		reason = "";
		try
		{
			if (party == null)
			{
				reason = label + "_null";
				return false;
			}
			if (!party.IsActive)
			{
				reason = label + "_inactive";
				return false;
			}
			if (party.Party == null)
			{
				reason = label + "_partybase_null";
				return false;
			}
			if (party.MemberRoster == null)
			{
				reason = label + "_member_roster_null";
				return false;
			}
			if (party.PrisonRoster == null)
			{
				reason = label + "_prison_roster_null";
				return false;
			}
			if (party.ItemRoster == null)
			{
				reason = label + "_item_roster_null";
				return false;
			}
			if (party.MapFaction == null)
			{
				reason = label + "_map_faction_null";
				return false;
			}
			if (party.Party.Culture == null)
			{
				reason = label + "_party_culture_null";
				return false;
			}
			Settlement partySettlement = party.Party.Settlement;
			if (partySettlement != null && partySettlement.Culture == null)
			{
				reason = label + "_party_settlement_culture_null";
				return false;
			}
			Hero owner = party.Party.Owner;
			if (owner != null && !ValidateHeroForNativePartyAi(owner, label + "_owner", requireClan: false, out reason))
			{
				return false;
			}
			Hero leader = party.LeaderHero;
			if (leader != null && !ValidateHeroForNativePartyAi(leader, label + "_leader", party.IsLordParty || leader.IsLord, out reason))
			{
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateHeroForNativePartyAi(Hero hero, string label, bool requireClan, out string reason)
	{
		reason = "";
		try
		{
			if (hero == null)
			{
				reason = label + "_null";
				return false;
			}
			if (hero.CharacterObject == null)
			{
				reason = label + "_character_null";
				return false;
			}
			if (hero.Culture == null)
			{
				reason = label + "_culture_null";
				return false;
			}
			if (hero.MapFaction == null)
			{
				reason = label + "_map_faction_null";
				return false;
			}
			if (requireClan && hero.Clan == null)
			{
				reason = label + "_clan_null";
				return false;
			}
			if (hero.Clan != null)
			{
				if (hero.Clan.Culture == null)
				{
					reason = label + "_clan_culture_null";
					return false;
				}
				if (hero.Clan.Leader != null && hero.Clan.Leader.CharacterObject == null)
				{
					reason = label + "_clan_leader_character_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateCampaignModelsForNativeVisit(out string reason)
	{
		reason = "";
		try
		{
			Campaign campaign = Campaign.Current;
			if (campaign == null || campaign.Models == null)
			{
				reason = "campaign_models_unavailable";
				return false;
			}
			if (campaign.Models.CampaignTimeModel == null)
			{
				reason = "campaign_time_model_null";
				return false;
			}
			if (campaign.Models.MobilePartyAIModel == null)
			{
				reason = "mobile_party_ai_model_null";
				return false;
			}
			if (campaign.Models.PartyFoodBuyingModel == null)
			{
				reason = "party_food_buying_model_null";
				return false;
			}
			if (campaign.Models.PartyWageModel == null)
			{
				reason = "party_wage_model_null";
				return false;
			}
			if (campaign.Models.SettlementGarrisonModel == null)
			{
				reason = "settlement_garrison_model_null";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "campaign_models_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateRosterForNativeWage(TroopRoster roster, string label, out string reason)
	{
		reason = "";
		try
		{
			if (roster == null)
			{
				reason = label + "_null";
				return false;
			}
			for (int i = 0; i < roster.Count; i++)
			{
				TroopRosterElement element = roster.GetElementCopyAtIndex(i);
				CharacterObject character = element.Character;
				if (character == null)
				{
					reason = label + "_character_null";
					return false;
				}
				if (character.IsHero)
				{
					if (character.HeroObject == null || character.HeroObject.CharacterObject == null)
					{
						reason = label + "_hero_invalid";
						return false;
					}
				}
				else if (character.Culture == null)
				{
					reason = label + "_culture_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateRosterForNativeUpgrade(TroopRoster roster, string label, out string reason)
	{
		if (!ValidateRosterForNativeWage(roster, label, out reason))
		{
			return false;
		}
		try
		{
			for (int i = 0; i < roster.Count; i++)
			{
				CharacterObject character = roster.GetElementCopyAtIndex(i).Character;
				if (character?.UpgradeTargets == null)
				{
					reason = label + "_upgrade_targets_null";
					return false;
				}
				foreach (CharacterObject target in character.UpgradeTargets)
				{
					if (target == null)
					{
						reason = label + "_upgrade_target_null";
						return false;
					}
					if (target.Culture == null)
					{
						reason = label + "_upgrade_target_culture_null";
						return false;
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_upgrade_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static MobileParty ExtractMobileParty(PartyBase partyBase)
	{
		try
		{
			return partyBase?.MobileParty;
		}
		catch
		{
			return null;
		}
	}

	private static PartyBase GetShipOwnerSafe(Ship ship)
	{
		try
		{
			return ship?.Owner;
		}
		catch
		{
			return null;
		}
	}

	private static string DescribeShip(Ship ship)
	{
		if (ship == null)
		{
			return "ship=null";
		}
		try
		{
			return "hull=" + (ship.ShipHull?.StringId ?? "null") +
				" owner=" + (ship.Owner?.MobileParty?.StringId ?? ship.Owner?.Settlement?.StringId ?? "null") +
				" hp=" + ship.HitPoints.ToString("0.##") + "/" + ship.MaxHitPoints.ToString("0.##");
		}
		catch (Exception ex)
		{
			return "ship_describe_failed=" + ex.GetType().Name;
		}
	}

	private static bool IsUnsafeForNativeAiVisitSettlement(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			if (party == null)
			{
				reason = "party_null";
				return true;
			}
			if (!party.IsActive)
			{
				reason = "party_inactive";
				return true;
			}
			if (party.Party == null)
			{
				reason = "partybase_null";
				return true;
			}
			if (Campaign.Current == null || Campaign.Current.Models == null)
			{
				reason = "campaign_unavailable";
				return true;
			}
			if (!ValidateCampaignModelsForNativeVisit(out reason))
			{
				return true;
			}
			IFaction mapFaction = party.MapFaction;
			if (mapFaction == null)
			{
				reason = "map_faction_null";
				return true;
			}
			if (!ValidateBasicSettlementReference(party.CurrentSettlement, "current_settlement", out reason)
				|| !ValidateBasicSettlementReference(party.TargetSettlement, "target_settlement", out reason)
				|| !ValidateBasicSettlementReference(party.LastVisitedSettlement, "last_visited_settlement", out reason))
			{
				return true;
			}
			if (!ValidatePartyRostersForNativeVisit(party, "party", out reason))
			{
				return true;
			}
			if (party.IsBandit)
			{
				if (party.Party.Culture == null || mapFaction.Culture == null)
				{
					reason = "bandit_culture_null";
					return true;
				}
				if (!ValidateItemRosterForNativeVisit(party, "bandit_item_roster", out reason)
					|| !ValidateBanditHideoutInputsForNativeVisit(party, out reason))
				{
					return true;
				}
				return false;
			}
			if (WillNativeAiVisitSettlementReturnBeforeRiskyReads(party, mapFaction))
			{
				return false;
			}
			Hero owner = party.Party.Owner;
			if (owner == null)
			{
				reason = "party_owner_null";
				return true;
			}
			if (!ValidateHeroForNativePartyAi(owner, "party_owner", requireClan: false, out reason))
			{
				return true;
			}
			Hero leader = party.LeaderHero;
			if (leader != null)
			{
				if (!ValidateHeroForNativePartyAi(leader, "leader", party.IsLordParty || leader.IsLord, out reason))
				{
					return true;
				}
				Hero clanLeader = leader.Clan?.Leader;
				if (clanLeader != null && !ValidateHeroForNativePartyAi(clanLeader, "leader_clan_leader", requireClan: false, out reason))
				{
					return true;
				}
				MobileParty clanLeaderParty = clanLeader?.PartyBelongedTo;
				if (clanLeaderParty != null && !ValidateMobilePartyForNativeDailySystems(clanLeaderParty, "leader_clan_leader_party", out reason))
				{
					return true;
				}
			}
			if (party.MemberRoster == null)
			{
				reason = "member_roster_null";
				return true;
			}
			if (party.PrisonRoster == null)
			{
				reason = "prison_roster_null";
				return true;
			}
			if (party.ItemRoster == null)
			{
				reason = "item_roster_null";
				return true;
			}
			if (!ValidateArmyForNativeVisit(party, out reason)
				|| !ValidateItemRosterForNativeVisit(party, "item_roster", out reason)
				|| !ValidatePrisonRosterHeroClans(party, "prison_roster", out reason)
				|| !ValidateShipsForNativeVisit(party, out reason)
				|| !ValidateCandidateSettlementsForNativeVisit(party, mapFaction, leader, out reason))
			{
				return true;
			}
			return false;
		}
		catch (Exception ex)
		{
			reason = "guard_exception:" + ex.GetType().Name;
			return true;
		}
	}

	private static bool WillNativeAiVisitSettlementReturnBeforeRiskyReads(MobileParty party, IFaction mapFaction)
	{
		try
		{
			if (party.CurrentSettlement?.SiegeEvent != null)
			{
				return true;
			}
			if (party.IsMilitia || party.IsCaravan || party.IsPatrolParty || party.IsVillager)
			{
				return true;
			}
			Hero leader = party.LeaderHero;
			if (!mapFaction.IsMinorFaction && !mapFaction.IsKingdomFaction && (leader == null || !leader.IsLord))
			{
				return true;
			}
			if (party.Army != null && party.AttachedTo != null && party.Army.LeaderParty != party)
			{
				return true;
			}
		}
		catch
		{
			return false;
		}
		return false;
	}

	private static bool ValidatePartyRostersForNativeVisit(MobileParty party, string label, out string reason)
	{
		reason = "";
		if (party == null)
		{
			reason = label + "_null";
			return false;
		}
		if (!party.IsActive)
		{
			reason = label + "_inactive";
			return false;
		}
		if (party.Party == null)
		{
			reason = label + "_partybase_null";
			return false;
		}
		if (party.MemberRoster == null)
		{
			reason = label + "_member_roster_null";
			return false;
		}
		if (party.PrisonRoster == null)
		{
			reason = label + "_prison_roster_null";
			return false;
		}
		if (party.ItemRoster == null)
		{
			reason = label + "_item_roster_null";
			return false;
		}
		if (party.Party.PrisonerSizeLimit <= 0)
		{
			reason = label + "_prisoner_limit_nonpositive";
			return false;
		}
		return true;
	}

	private static bool ValidateArmyForNativeVisit(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			Army army = party?.Army;
			if (army == null)
			{
				return true;
			}
			if (army.LeaderParty == null)
			{
				reason = "army_leader_null";
				return false;
			}
			if (army.Parties == null || army.Parties.Count <= 0)
			{
				reason = "army_parties_empty";
				return false;
			}
			if (!ValidatePartyRostersForNativeVisit(army.LeaderParty, "army_leader", out reason))
			{
				return false;
			}
			if (army.LeaderParty.AttachedParties == null)
			{
				reason = "army_leader_attached_parties_null";
				return false;
			}
			int prisonerLimit = party.Party.PrisonerSizeLimit;
			foreach (MobileParty attachedParty in army.LeaderParty.AttachedParties)
			{
				if (!ValidatePartyRostersForNativeVisit(attachedParty, "army_attached", out reason))
				{
					return false;
				}
				if (!ValidatePrisonRosterHeroClans(attachedParty, "army_attached_prison_roster", out reason))
				{
					return false;
				}
				prisonerLimit += attachedParty.Party.PrisonerSizeLimit;
			}
			if (prisonerLimit <= 0)
			{
				reason = "army_prisoner_limit_nonpositive";
				return false;
			}
			if (party.AttachedParties == null)
			{
				reason = "party_attached_parties_null";
				return false;
			}
			foreach (MobileParty attachedParty in party.AttachedParties)
			{
				if (!ValidatePartyRostersForNativeVisit(attachedParty, "party_attached", out reason))
				{
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "army_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateItemRosterForNativeVisit(MobileParty party, string label, out string reason)
	{
		reason = "";
		try
		{
			if (party?.ItemRoster == null)
			{
				reason = label + "_null";
				return false;
			}
			for (int i = 0; i < party.ItemRoster.Count; i++)
			{
				ItemRosterElement element = party.ItemRoster[i];
				if (element.EquipmentElement.Item == null)
				{
					reason = label + "_item_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateSettlementItemRosterForNativeVisit(Settlement settlement, string label, out string reason)
	{
		reason = "";
		try
		{
			if (settlement?.ItemRoster == null)
			{
				reason = label + "_null";
				return false;
			}
			for (int i = 0; i < settlement.ItemRoster.Count; i++)
			{
				ItemRosterElement element = settlement.ItemRoster[i];
				if (element.EquipmentElement.Item == null)
				{
					reason = label + "_item_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidatePrisonRosterHeroClans(MobileParty party, string label, out string reason)
	{
		reason = "";
		try
		{
			if (party?.PrisonRoster == null)
			{
				reason = label + "_null";
				return false;
			}
			if (party.PrisonRoster.TotalHeroes <= 0)
			{
				return true;
			}
			foreach (TroopRosterElement element in party.PrisonRoster.GetTroopRoster())
			{
				if (element.Character == null)
				{
					reason = label + "_character_null";
					return false;
				}
				if (element.Character.IsHero && (element.Character.HeroObject == null || element.Character.HeroObject.Clan == null))
				{
					reason = label + "_hero_clan_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateShipsForNativeVisit(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			if (party?.Ships == null)
			{
				reason = "ships_null";
				return false;
			}
			foreach (var ship in party.Ships)
			{
				if (ship == null)
				{
					reason = "ship_null";
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "ships_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateCandidateSettlementsForNativeVisit(MobileParty party, IFaction mapFaction, Hero leader, out string reason)
	{
		reason = "";
		try
		{
			if (!ValidateLikelyNativeVisitSettlementCandidate(party, mapFaction?.FactionMidSettlement, "faction_mid_settlement", out bool validateFactionMidSettlement, out reason))
			{
				return false;
			}
			if (validateFactionMidSettlement && !ValidateNativeVisitSettlementCandidate(mapFaction?.FactionMidSettlement, "faction_mid_settlement", out reason))
			{
				return false;
			}
			if (leader != null && leader.MapFaction?.IsKingdomFaction == true)
			{
				if (mapFaction.Settlements == null)
				{
					reason = "map_faction_settlements_null";
					return false;
				}
				int checkedCount = 0;
				foreach (Settlement settlement in mapFaction.Settlements)
				{
					if (checkedCount++ >= MaxFactionSettlementsChecked)
					{
						break;
					}
					if (!ValidateLikelyNativeVisitSettlementCandidate(party, settlement, "map_faction_settlement", out bool shouldValidate, out reason))
					{
						return false;
					}
					if (shouldValidate && !ValidateNativeVisitSettlementCandidate(settlement, "map_faction_settlement", out reason))
					{
						return false;
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "settlement_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateBanditHideoutInputsForNativeVisit(MobileParty party, out string reason)
	{
		reason = "";
		try
		{
			if (Hideout.All == null)
			{
				reason = "hideouts_null";
				return false;
			}
			foreach (Hideout hideout in Hideout.All)
			{
				if (hideout == null)
				{
					reason = "hideout_null";
					return false;
				}
				if (!ValidateNativeVisitSettlementCandidate(hideout.Settlement, "hideout_settlement", out reason))
				{
					return false;
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "hideout_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateLikelyNativeVisitSettlementCandidate(MobileParty party, Settlement settlement, string label, out bool shouldValidate, out string reason)
	{
		shouldValidate = false;
		reason = "";
		try
		{
			if (settlement == null)
			{
				return true;
			}
			if (!ValidateBasicSettlementReference(settlement, label, out reason))
			{
				return false;
			}
			if (!(settlement.IsVillage || settlement.IsFortification))
			{
				return true;
			}
			if (settlement.Party.MapEvent != null)
			{
				return true;
			}
			if (settlement.Party.SiegeEvent != null && (settlement.Party.SiegeEvent.IsBlockadeActive || party?.HasNavalNavigationCapability != true))
			{
				return true;
			}
			IFaction ownerFaction = party?.Party?.Owner?.MapFaction;
			if (ownerFaction == null)
			{
				reason = label + "_owner_faction_null";
				return false;
			}
			bool canVisitEnemyVillageFallback = false;
			try
			{
				canVisitEnemyVillageFallback = (ownerFaction.IsMinorFaction || party.MapFaction?.Settlements?.Count == 0) && settlement.IsVillage;
			}
			catch
			{
				canVisitEnemyVillageFallback = false;
			}
			if (ownerFaction.IsAtWarWith(settlement.MapFaction) && !canVisitEnemyVillageFallback)
			{
				return true;
			}
			if (settlement.IsVillage)
			{
				if (settlement.Village == null)
				{
					reason = label + "_village_null";
					return false;
				}
				if (settlement.Village.VillageState != Village.VillageStates.Normal)
				{
					return true;
				}
			}
			shouldValidate = true;
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_candidate_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateBasicSettlementReference(Settlement settlement, string label, out string reason)
	{
		reason = "";
		try
		{
			if (settlement == null)
			{
				return true;
			}
			if (!settlement.IsActive)
			{
				reason = label + "_inactive";
				return false;
			}
			if (settlement.Party == null)
			{
				reason = label + "_party_null";
				return false;
			}
			if (settlement.MapFaction == null)
			{
				reason = label + "_map_faction_null";
				return false;
			}
			if (settlement.Culture == null)
			{
				reason = label + "_culture_null";
				return false;
			}
			if (settlement.Party.Culture == null)
			{
				reason = label + "_party_culture_null";
				return false;
			}
			if (settlement.IsVillage && settlement.Village == null)
			{
				reason = label + "_village_null";
				return false;
			}
			if (settlement.IsFortification && settlement.Town == null)
			{
				reason = label + "_town_null";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool ValidateNativeVisitSettlementCandidate(Settlement settlement, string label, out string reason)
	{
		reason = "";
		if (settlement == null)
		{
			return true;
		}
		try
		{
			if (!ValidateBasicSettlementReference(settlement, label, out reason))
			{
				return false;
			}
			if (!(settlement.IsVillage || settlement.IsFortification || settlement.IsHideout))
			{
				return true;
			}
			if (settlement.ItemRoster == null)
			{
				reason = label + "_item_roster_null";
				return false;
			}
			if (!ValidateSettlementItemRosterForNativeVisit(settlement, label + "_item_roster", out reason))
			{
				return false;
			}
			if (settlement.IsVillage && settlement.Village.Bound == null)
			{
				reason = label + "_village_bound_null";
				return false;
			}
			Clan ownerClan = settlement.OwnerClan;
			if (!settlement.IsHideout && ownerClan == null)
			{
				reason = label + "_owner_clan_null";
				return false;
			}
			if (!settlement.IsHideout && ownerClan.Leader == null)
			{
				reason = label + "_owner_clan_leader_null";
				return false;
			}
			if (!settlement.IsHideout && !ValidateHeroForNativePartyAi(ownerClan.Leader, label + "_owner_clan_leader", requireClan: false, out reason))
			{
				return false;
			}
			if (settlement.IsFortification && settlement.Town?.GarrisonParty != null && !ValidateMobilePartyForNativeDailySystems(settlement.Town.GarrisonParty, label + "_garrison_party", out reason))
			{
				return false;
			}
			if (settlement.Notables == null)
			{
				reason = label + "_notables_null";
				return false;
			}
			foreach (Hero notable in settlement.Notables)
			{
				if (notable == null)
				{
					reason = label + "_notable_null";
					return false;
				}
				if (notable.VolunteerTypes == null || notable.VolunteerTypes.Length < 4)
				{
					reason = label + "_volunteers_invalid";
					return false;
				}
			}
			if (settlement.BoundVillages == null)
			{
				reason = label + "_bound_villages_null";
				return false;
			}
			foreach (Village village in settlement.BoundVillages)
			{
				if (village == null || village.Settlement == null)
				{
					reason = label + "_bound_village_invalid";
					return false;
				}
			}
			if (settlement.Parties == null)
			{
				reason = label + "_parties_null";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = label + "_guard_exception:" + ex.GetType().Name;
			return false;
		}
	}

	private static bool IsRecoverableNativeAiStateException(Exception exception)
	{
		return exception is NullReferenceException
			|| exception is InvalidOperationException
			|| exception is ArgumentException
			|| exception is IndexOutOfRangeException
			|| exception is KeyNotFoundException
			|| exception is DivideByZeroException;
	}

	private static bool StartsWithAny(string value, params string[] prefixes)
	{
		if (string.IsNullOrWhiteSpace(value) || prefixes == null)
		{
			return false;
		}
		for (int i = 0; i < prefixes.Length; i++)
		{
			if (!string.IsNullOrEmpty(prefixes[i]) && value.StartsWith(prefixes[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static void TryLockNativeAiDecisions(MobileParty party, string reason)
	{
		try
		{
			if (party?.Ai != null && !party.Ai.DoNotMakeNewDecisions)
			{
				party.Ai.SetDoNotMakeNewDecisions(true);
				LogGuard("native_decisions_locked", party, reason);
			}
		}
		catch (Exception ex)
		{
			LogGuard("native_decisions_lock_failed", party, reason, ex);
		}
	}

	private static void TryDelayNativeAiDecisionOneTick(MobileParty party, string reason)
	{
		try
		{
			if (party?.Ai == null)
			{
				return;
			}
			party.Ai.RethinkAtNextHourlyTick = false;
			party.Ai.HourCounter++;
		}
		catch (Exception ex)
		{
			LogGuard("native_decision_delay_failed", party, reason, ex);
		}
	}

	private static void LogGuard(string stage, MobileParty party, string reason, Exception exception = null, MethodBase method = null)
	{
		try
		{
			string partyId = party?.StringId ?? "null";
			string key = (stage ?? "") + "|" + partyId + "|" + (reason ?? "") + "|" + (exception?.GetType().Name ?? "");
			lock (LoggedKeys)
			{
				if (LoggedKeys.Contains(key))
				{
					return;
				}
				if (LoggedKeys.Count >= MaxLoggedKeys)
				{
					return;
				}
				LoggedKeys.Add(key);
			}
			Logger.Log(LogSource,
				"stage=" + (stage ?? "") +
				" reason=" + (reason ?? "") +
				" party=" + DescribeParty(party) +
				(exception == null ? "" : " exception=" + exception.GetType().Name + ":" + exception.Message) +
				(method == null ? "" : " method=" + method.DeclaringType?.FullName + "." + method.Name));
		}
		catch
		{
		}
	}

	private static string DescribeParty(MobileParty party)
	{
		if (party == null)
		{
			return "null";
		}
		try
		{
			return (party.StringId ?? "no_id") +
				" leader=" + (party.LeaderHero?.StringId ?? "null") +
				" owner=" + (party.Party?.Owner?.StringId ?? "null") +
				" faction=" + (party.MapFaction?.StringId ?? "null") +
				" default=" + party.DefaultBehavior +
				" short=" + party.ShortTermBehavior +
				" targetSettlement=" + (party.TargetSettlement?.StringId ?? "null") +
				" component=" + (party.PartyComponent?.GetType().FullName ?? "null");
		}
		catch (Exception ex)
		{
			return (party.StringId ?? "no_id") + " describe_failed=" + ex.GetType().Name;
		}
	}
}
