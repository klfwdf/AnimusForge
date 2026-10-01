using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;

namespace AnimusForge;

internal static class PermanentAllianceGuard
{
	private const string KingdomVoteWarSource = "kingdom_vote_declared_war";

	internal sealed class BreakAuthorization
	{
		public string FirstKingdomId = "";
		public string SecondKingdomId = "";
		public string Source = "";
	}

	internal readonly struct AuthorizationScope : IDisposable
	{
		private readonly BreakAuthorization _authorization;

		internal AuthorizationScope(BreakAuthorization authorization)
		{
			_authorization = authorization;
		}

		public void Dispose()
		{
			if (_authorization == null || _authorizationStack == null) return;
			if (_authorizationStack.Count > 0 && ReferenceEquals(_authorizationStack.Peek(), _authorization))
			{
				_authorizationStack.Pop();
			}
			else
			{
				BreakAuthorization[] entries = _authorizationStack.ToArray();
				_authorizationStack.Clear();
				for (int index = entries.Length - 1; index >= 0; index--)
				{
					if (!ReferenceEquals(entries[index], _authorization)) _authorizationStack.Push(entries[index]);
				}
			}
		}
	}

	private static readonly FieldInfo AlliancesField = typeof(AllianceCampaignBehavior)
		.GetField("_alliances", BindingFlags.Instance | BindingFlags.NonPublic);
	private static readonly HashSet<string> ExplicitBreakSources = new HashSet<string>(StringComparer.Ordinal)
	{
		"world_diplomacy_break_alliance",
		"diplomacy_break_alliance"
	};
	private static readonly object LogSync = new object();
	private static readonly Dictionary<string, int> LastBlockedLogDayByPair =
		new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	private static bool _patchRegistrationAttempted;
	private static bool _reflectionFailureLogged;
	private static int _lastGlobalRefreshDay = int.MinValue;
	private static Type _allianceEntryType;
	private static FieldInfo _allianceFirstKingdomField;
	private static FieldInfo _allianceSecondKingdomField;
	private static FieldInfo _allianceEndTimeField;

	[ThreadStatic]
	private static Stack<BreakAuthorization> _authorizationStack;

	internal static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null || _patchRegistrationAttempted) return;
		_patchRegistrationAttempted = true;
		bool endAlliancePatched = TryPatch(harmony, typeof(Patch_AllianceCampaignBehavior_EndAlliance));
		bool dailyTickPatched = TryPatch(harmony, typeof(Patch_AllianceCampaignBehavior_DailyTickClan));
		bool startAlliancePatched = TryPatch(harmony, typeof(Patch_AllianceCampaignBehavior_StartAlliance));
		bool voteOutcomePatched = TryPatch(harmony, typeof(Patch_DeclareWarDecision_ApplyChosenOutcome));
		if (!endAlliancePatched)
		{
			Logger.Log("PermanentAlliance",
				"FATAL: EndAlliance guard patch failed; permanent alliances are not protected from implicit cancellation.");
		}
		if (endAlliancePatched && dailyTickPatched && startAlliancePatched && voteOutcomePatched)
		{
			Logger.Log("PermanentAlliance", "All permanent alliance guard patches applied successfully.");
			return;
		}
		Logger.Log("PermanentAlliance", "Permanent alliance patch registration incomplete: EndAlliance="
			+ endAlliancePatched + " DailyTickClan=" + dailyTickPatched
			+ " StartAlliance=" + startAlliancePatched + " DeclareWarVoteOutcome=" + voteOutcomePatched);
	}

	private static bool TryPatch(Harmony harmony, Type patchType)
	{
		try
		{
			List<MethodInfo> patchedMethods = harmony.CreateClassProcessor(patchType).Patch();
			if (patchedMethods == null || patchedMethods.Count == 0)
			{
				Logger.Log("PermanentAlliance", "Permanent alliance patch produced no patched methods type=" + patchType.Name);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("PermanentAlliance", "Permanent alliance patch failed type="
				+ patchType.Name + " error=" + ex.Message);
			return false;
		}
	}

	internal static void RunAuthorizedBreak(string source, Kingdom first, Kingdom second, Action action)
	{
		if (action == null || first == null || second == null || !ExplicitBreakSources.Contains(source ?? ""))
		{
			Logger.Log("PermanentAlliance", "Rejected unknown alliance-break authorization source="
				+ (source ?? "") + " pair=" + PairKey(first, second));
			return;
		}
		using (BeginAuthorizedBreak(source, first, second)) action();
	}

	private static AuthorizationScope BeginAuthorizedBreak(string source, Kingdom first, Kingdom second)
	{
		BreakAuthorization authorization = new BreakAuthorization
		{
			FirstKingdomId = first?.StringId ?? "",
			SecondKingdomId = second?.StringId ?? "",
			Source = source ?? ""
		};
		_authorizationStack ??= new Stack<BreakAuthorization>();
		_authorizationStack.Push(authorization);
		return new AuthorizationScope(authorization);
	}

	internal static bool ShouldAllowDeclareWar(
		IFaction first,
		IFaction second,
		DeclareWarAction.DeclareWarDetail detail)
	{
		if (first is not Kingdom firstKingdom || second is not Kingdom secondKingdom) return true;
		IAllianceCampaignBehavior alliances = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
		if (alliances?.IsAllyWithKingdom(firstKingdom, secondKingdom) != true) return true;
		if (detail == DeclareWarAction.DeclareWarDetail.CausedByKingdomDecision
			&& IsAuthorizedBySource(firstKingdom, secondKingdom, KingdomVoteWarSource))
		{
			return true;
		}
		Logger.Log("PermanentAlliance", "Blocked war against an allied kingdom without a matching approved-vote scope pair="
			+ PairKey(firstKingdom, secondKingdom) + " detail=" + detail);
		return false;
	}

	private static bool IsAuthorizedBySource(Kingdom first, Kingdom second, string requiredSource)
	{
		if (first == null || second == null || string.IsNullOrEmpty(requiredSource)
			|| _authorizationStack == null || _authorizationStack.Count == 0) return false;
		foreach (BreakAuthorization authorization in _authorizationStack)
		{
			if (string.Equals(authorization.Source, requiredSource, StringComparison.Ordinal)
				&& PairMatches(authorization.FirstKingdomId, authorization.SecondKingdomId, first.StringId, second.StringId))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsAuthorized(Kingdom first, Kingdom second, out string source)
	{
		source = "";
		if (first == null || second == null || _authorizationStack == null || _authorizationStack.Count == 0) return false;
		foreach (BreakAuthorization authorization in _authorizationStack)
		{
			if ((ExplicitBreakSources.Contains(authorization.Source ?? "")
					|| string.Equals(authorization.Source, KingdomVoteWarSource, StringComparison.Ordinal))
				&& PairMatches(authorization.FirstKingdomId, authorization.SecondKingdomId, first.StringId, second.StringId))
			{
				source = authorization.Source;
				return true;
			}
		}
		return false;
	}

	private static bool PairMatches(string firstA, string secondA, string firstB, string secondB)
	{
		return (string.Equals(firstA, firstB, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(secondA, secondB, StringComparison.OrdinalIgnoreCase))
			|| (string.Equals(firstA, secondB, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(secondA, firstB, StringComparison.OrdinalIgnoreCase));
	}

	private static string PairKey(Kingdom first, Kingdom second)
	{
		string firstId = first?.StringId ?? "";
		string secondId = second?.StringId ?? "";
		return string.Compare(firstId, secondId, StringComparison.OrdinalIgnoreCase) <= 0
			? firstId + "|" + secondId
			: secondId + "|" + firstId;
	}

	private static int CurrentDay()
	{
		try
		{
			return Math.Max(0, (int)CampaignTime.Now.ToDays);
		}
		catch
		{
			return 0;
		}
	}

	private static bool EnsureAllianceEntryFields(Type entryType)
	{
		if (entryType == null) return false;
		if (_allianceEntryType == entryType)
		{
			return _allianceFirstKingdomField != null
				&& _allianceSecondKingdomField != null
				&& _allianceEndTimeField != null;
		}
		_allianceEntryType = entryType;
		_allianceFirstKingdomField = entryType.GetField("Kingdom1", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_allianceSecondKingdomField = entryType.GetField("Kingdom2", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		_allianceEndTimeField = entryType.GetField("EndTime", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		return _allianceFirstKingdomField != null
			&& _allianceSecondKingdomField != null
			&& _allianceEndTimeField != null;
	}

	private static void RefreshAllAllianceEndTimes(AllianceCampaignBehavior behavior)
	{
		if (behavior == null || Campaign.Current == null) return;
		int day = CurrentDay();
		if (_lastGlobalRefreshDay == day) return;
		_lastGlobalRefreshDay = day;
		RefreshAllianceEndTimes(behavior, null, null);
	}

	private static void RefreshAllianceEndTime(AllianceCampaignBehavior behavior, Kingdom first, Kingdom second)
	{
		if (behavior == null || first == null || second == null || Campaign.Current == null) return;
		RefreshAllianceEndTimes(behavior, first, second);
	}

	private static void RefreshAllianceEndTimes(AllianceCampaignBehavior behavior, Kingdom first, Kingdom second)
	{
		try
		{
			if (AlliancesField?.GetValue(behavior) is not IList alliances) return;
			CampaignTime renewedEnd = CampaignTime.Now + Campaign.Current.Models.AllianceModel.MaxDurationOfAlliance;
			for (int index = 0; index < alliances.Count; index++)
			{
				object boxedAlliance = alliances[index];
				if (boxedAlliance == null) continue;
				if (!EnsureAllianceEntryFields(boxedAlliance.GetType())) continue;
				Kingdom storedFirst = _allianceFirstKingdomField.GetValue(boxedAlliance) as Kingdom;
				Kingdom storedSecond = _allianceSecondKingdomField.GetValue(boxedAlliance) as Kingdom;
				if (first != null && second != null
					&& !PairMatches(storedFirst?.StringId, storedSecond?.StringId, first.StringId, second.StringId)) continue;
				_allianceEndTimeField.SetValue(boxedAlliance, renewedEnd);
				alliances[index] = boxedAlliance;
			}
		}
		catch (Exception ex)
		{
			if (_reflectionFailureLogged) return;
			_reflectionFailureLogged = true;
			Logger.Log("PermanentAlliance", "Alliance end-time refresh failed; EndAlliance guard remains active. error=" + ex.Message);
		}
	}

	private static void LogBlockedEnd(Kingdom first, Kingdom second)
	{
		string pair = PairKey(first, second);
		int day = CurrentDay();
		lock (LogSync)
		{
			if (LastBlockedLogDayByPair.TryGetValue(pair, out int lastDay) && lastDay == day) return;
			LastBlockedLogDayByPair[pair] = day;
		}
		Logger.Log("PermanentAlliance", "Blocked implicit alliance cancellation pair=" + pair + " day=" + day);
	}

	[HarmonyPatch(typeof(AllianceCampaignBehavior), "DailyTickClan")]
	private static class Patch_AllianceCampaignBehavior_DailyTickClan
	{
		private static void Prefix(AllianceCampaignBehavior __instance, Clan clan)
		{
			if (clan?.Kingdom == null || clan.Kingdom.RulingClan != clan) return;
			RefreshAllAllianceEndTimes(__instance);
		}
	}

	[HarmonyPatch(typeof(AllianceCampaignBehavior), nameof(AllianceCampaignBehavior.StartAlliance))]
	private static class Patch_AllianceCampaignBehavior_StartAlliance
	{
		private static void Postfix(AllianceCampaignBehavior __instance, Kingdom proposerKingdom, Kingdom receiverKingdom)
		{
			RefreshAllianceEndTime(__instance, proposerKingdom, receiverKingdom);
		}
	}

	[HarmonyPatch(typeof(AllianceCampaignBehavior), nameof(AllianceCampaignBehavior.EndAlliance))]
	private static class Patch_AllianceCampaignBehavior_EndAlliance
	{
		private static bool Prefix(AllianceCampaignBehavior __instance, Kingdom kingdom1, Kingdom kingdom2)
		{
			if (kingdom1 == null || kingdom2 == null) return true;
			if (kingdom1.IsEliminated || kingdom2.IsEliminated) return true;
			if (FactionManager.IsAtWarAgainstFaction(kingdom1, kingdom2)) return true;
			if (__instance?.IsAllyWithKingdom(kingdom1, kingdom2) != true) return true;
			if (IsAuthorized(kingdom1, kingdom2, out string source))
			{
				Logger.Log("PermanentAlliance", "Allowed explicit alliance cancellation pair="
					+ PairKey(kingdom1, kingdom2) + " source=" + source);
				return true;
			}
			RefreshAllianceEndTime(__instance, kingdom1, kingdom2);
			LogBlockedEnd(kingdom1, kingdom2);
			return false;
		}
	}

	[HarmonyPatch(typeof(DeclareWarDecision), nameof(DeclareWarDecision.ApplyChosenOutcome))]
	private static class Patch_DeclareWarDecision_ApplyChosenOutcome
	{
		private static void Prefix(
			DeclareWarDecision __instance,
			DecisionOutcome chosenOutcome,
			out AuthorizationScope __state)
		{
			__state = default;
			if (__instance == null
				|| chosenOutcome is not DeclareWarDecision.DeclareWarDecisionOutcome outcome
				|| !outcome.ShouldWarBeDeclared) return;
			Kingdom decisionKingdom = __instance.Kingdom;
			IFaction decisionTarget = __instance.FactionToDeclareWarOn;
			if (outcome.Kingdom == null
				|| decisionTarget is not Kingdom targetKingdom
				|| !ReferenceEquals(outcome.Kingdom, decisionKingdom)
				|| !ReferenceEquals(outcome.FactionToDeclareWarOn, decisionTarget))
			{
				Logger.Log("PermanentAlliance", "Rejected mismatched DeclareWarDecision yes outcome vote scope.");
				return;
			}
			__state = BeginAuthorizedBreak(KingdomVoteWarSource, outcome.Kingdom, targetKingdom);
		}

		private static Exception Finalizer(Exception __exception, AuthorizationScope __state)
		{
			__state.Dispose();
			return __exception;
		}
	}
}
