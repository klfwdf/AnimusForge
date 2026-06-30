using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

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
			PatchAiVisitSettlementTick(harmony);
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
			if (!ShouldSkipNativeAiForUtilityParty(party, out string reason))
			{
				return true;
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
			if (ShouldSkipNativeAiForUtilityParty(party, out string utilityReason) || IsUnsafeForNativeAiVisitSettlement(party, out utilityReason))
			{
				LogGuard("visit_settlement_exception_suppressed", party, utilityReason, __exception, __originalMethod);
				return null;
			}
			if (IsRecoverableNativeAiStateException(__exception))
			{
				string reason = "native_state_exception:" + __exception.GetType().Name;
				LogGuard("visit_settlement_exception_suppressed", party, reason, __exception, __originalMethod);
				return null;
			}
		}
		catch
		{
		}
		return __exception;
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
		harmony.Patch(target, prefix: new HarmonyMethod(typeof(AnimusForgeMobilePartyAiSafetyPatch), nameof(PartyHourlyAiTickPrefix)));
		Logger.Log(LogSource, "AiPartyThinkBehavior.PartyHourlyAiTick utility party guard applied.");
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
		if (CourierDeliveryBehavior.IsCourierParty(party))
		{
			reason = "courier";
			return true;
		}
		if (NobleGatheringBehavior.IsTemporaryGatheringParty(party))
		{
			reason = "noble_gathering_temp";
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
			if (owner.MapFaction == null)
			{
				reason = "party_owner_faction_null";
				return true;
			}
			Hero leader = party.LeaderHero;
			if (leader != null)
			{
				if (leader.MapFaction == null)
				{
					reason = "leader_faction_null";
					return true;
				}
				if (leader.Clan == null)
				{
					reason = "leader_clan_null";
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
