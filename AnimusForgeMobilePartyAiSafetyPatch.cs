using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;

namespace AnimusForge;

internal static class AnimusForgeMobilePartyAiSafetyPatch
{
	private const string LogSource = "MobilePartyAiSafety";
	private const int MaxLoggedKeys = 128;

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
			if (party.Party == null)
			{
				reason = "partybase_null";
				return true;
			}
			IFaction mapFaction = party.MapFaction;
			if (mapFaction == null)
			{
				reason = "map_faction_null";
				return true;
			}
			if (party.IsBandit)
			{
				if (party.Party.Culture == null || mapFaction.Culture == null)
				{
					reason = "bandit_culture_null";
					return true;
				}
				return false;
			}
			if (WillNativeAiVisitSettlementReturnBeforeRiskyReads(party, mapFaction))
			{
				return false;
			}
			if (party.Army != null && party.Army.LeaderParty == null)
			{
				reason = "army_leader_null";
				return true;
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
