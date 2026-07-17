using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Mission-scoped castle disposition ledger. Deferred regular-prisoner outcomes own exact
/// roster subsets; nothing is removed from the campaign roster until mission exit.
/// </summary>
internal static class CastleAftermathDispositionSessionBridge
{
	private static readonly HashSet<SiegeCastleActionKind> RegularProcessActions = new HashSet<SiegeCastleActionKind>();
	private static readonly Dictionary<SiegeCastleActionKind, TroopRoster> RegularDeferredAllocations = new Dictionary<SiegeCastleActionKind, TroopRoster>();
	private static readonly List<SiegeCastleActionKind> RegularAllocationOrder = new List<SiegeCastleActionKind>();
	private static readonly List<SiegeCastleDispositionSummaryEntry> FinalizedRegularOutcomes = new List<SiegeCastleDispositionSummaryEntry>();
	private static readonly Dictionary<string, HashSet<SiegeCastleActionKind>> LordAppliedActions = new Dictionary<string, HashSet<SiegeCastleActionKind>>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, SiegeCastleActionKind> LordTerminalActions = new Dictionary<string, SiegeCastleActionKind>(StringComparer.OrdinalIgnoreCase);
	private static readonly List<string> LordOutcomeSummaries = new List<string>();
	private static TroopRoster _activeSlaughterTargets;
	private static SiegeCastleActionKind _lastRegularDispositionAction = SiegeCastleActionKind.Unknown;
	private static int _initialRegularPrisonerCount;
	private static int _revisionCount;

	internal static int InitialRegularPrisonerCount => Math.Max(0, _initialRegularPrisonerCount);

	internal static int RevisionCount => Math.Max(0, _revisionCount);

	internal static void Reset(string source)
	{
		RegularProcessActions.Clear();
		RegularDeferredAllocations.Clear();
		RegularAllocationOrder.Clear();
		FinalizedRegularOutcomes.Clear();
		LordAppliedActions.Clear();
		LordTerminalActions.Clear();
		LordOutcomeSummaries.Clear();
		_activeSlaughterTargets = null;
		_lastRegularDispositionAction = SiegeCastleActionKind.Unknown;
		_initialRegularPrisonerCount = 0;
		_revisionCount = 0;
		Logger.Log("CastleAftermath", "Reset castle disposition session ledger. Source=" + (source ?? "N/A"));
	}

	internal static void EnsureInitialRegularPrisonerCount(TroopRoster selectedRoster)
	{
		if (_initialRegularPrisonerCount <= 0)
		{
			_initialRegularPrisonerCount = CountRegular(selectedRoster);
		}
	}

	internal static void RecordLordOutcome(string summary)
	{
		string normalized = (summary ?? string.Empty).Trim();
		if (normalized.Length > 0 && !LordOutcomeSummaries.Contains(normalized))
		{
			LordOutcomeSummaries.Add(normalized);
		}
	}

	internal static string GetLordOutcomeSummary() => string.Join("；", LordOutcomeSummaries);

	internal static IReadOnlyCollection<SiegeCastleActionKind> GetAppliedActions(
		SiegeCastleActionSpeakerRole role,
		Agent agent,
		Hero hero)
	{
		if (role == SiegeCastleActionSpeakerRole.CapturedLord)
		{
			string key = ResolveLordKey(agent, hero);
			return !string.IsNullOrWhiteSpace(key) && LordAppliedActions.TryGetValue(key, out HashSet<SiegeCastleActionKind> actions)
				? actions.ToArray()
				: Array.Empty<SiegeCastleActionKind>();
		}
		if (role != SiegeCastleActionSpeakerRole.RegularPrisoner
			&& role != SiegeCastleActionSpeakerRole.AlliedSoldier)
		{
			return Array.Empty<SiegeCastleActionKind>();
		}
		return RegularProcessActions
			.Concat(RegularAllocationOrder)
			.Concat(_activeSlaughterTargets?.TotalManCount > 0
				? new[] { SiegeCastleActionKind.SlaughterPrisoners }
				: Array.Empty<SiegeCastleActionKind>())
			.Distinct()
			.ToArray();
	}

	internal static SiegeCastleActionKind GetTerminalAction(
		SiegeCastleActionSpeakerRole role,
		Agent agent,
		Hero hero)
	{
		if (role == SiegeCastleActionSpeakerRole.CapturedLord)
		{
			string key = ResolveLordKey(agent, hero);
			return !string.IsNullOrWhiteSpace(key) && LordTerminalActions.TryGetValue(key, out SiegeCastleActionKind action)
				? action
				: SiegeCastleActionKind.Unknown;
		}
		return role == SiegeCastleActionSpeakerRole.RegularPrisoner
			|| role == SiegeCastleActionSpeakerRole.AlliedSoldier
			? _lastRegularDispositionAction
			: SiegeCastleActionKind.Unknown;
	}

	internal static bool TryMarkApplied(
		SiegeCastleActionKind action,
		SiegeCastleActionSpeakerRole role,
		Agent agent,
		Hero hero)
	{
		if (!SiegeCastleActionKindProfile.IsSettlement(action)
			|| action == SiegeCastleActionKind.AppeaseSoldiers)
		{
			return true;
		}

		if (role == SiegeCastleActionSpeakerRole.CapturedLord)
		{
			string key = ResolveLordKey(agent, hero);
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}
			if (!LordAppliedActions.TryGetValue(key, out HashSet<SiegeCastleActionKind> actions))
			{
				actions = new HashSet<SiegeCastleActionKind>();
				LordAppliedActions[key] = actions;
			}
			if (!actions.Add(action))
			{
				return false;
			}
			if (SiegeCastleActionKindProfile.IsLordTerminal(action))
			{
				if (LordTerminalActions.ContainsKey(key))
				{
					actions.Remove(action);
					return false;
				}
				LordTerminalActions[key] = action;
			}
			return true;
		}

		if (role != SiegeCastleActionSpeakerRole.RegularPrisoner
			&& role != SiegeCastleActionSpeakerRole.AlliedSoldier)
		{
			return false;
		}
		return !SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action)
			&& RegularProcessActions.Add(action);
	}

	internal static TroopRoster GetUnassignedRegularRoster(TroopRoster selectedRoster)
	{
		EnsureInitialRegularPrisonerCount(selectedRoster);
		TroopRoster result = CloneRegularRoster(selectedRoster);
		foreach (TroopRoster allocation in RegularDeferredAllocations.Values)
		{
			SubtractRoster(result, allocation);
		}
		SubtractRoster(result, _activeSlaughterTargets);
		return result;
	}

	internal static void ResetRegularPlan(string source)
	{
		if (RegularDeferredAllocations.Count > 0 || (_activeSlaughterTargets?.TotalManCount ?? 0) > 0)
		{
			_revisionCount++;
		}
		RegularDeferredAllocations.Clear();
		RegularAllocationOrder.Clear();
		_activeSlaughterTargets = null;
		_lastRegularDispositionAction = SiegeCastleActionKind.Unknown;
		Logger.Log("CastleAftermath", "Reset staged regular-prisoner allocations. Revision=" + _revisionCount
			+ ", Source=" + (source ?? "N/A"));
	}

	internal static bool TryStageDeferredAllocation(
		SiegeCastleActionKind action,
		TroopRoster requestedRoster,
		out int stagedCount)
	{
		stagedCount = CountRegular(requestedRoster);
		if (!SiegeCastleRegularDispositionStagingProfile.IsDeferredRosterAction(action) || stagedCount <= 0)
		{
			return false;
		}
		if (!RegularDeferredAllocations.TryGetValue(action, out TroopRoster allocation))
		{
			allocation = TroopRoster.CreateDummyTroopRoster();
			RegularDeferredAllocations[action] = allocation;
			RegularAllocationOrder.Add(action);
		}
		AddRoster(allocation, requestedRoster);
		_lastRegularDispositionAction = action;
		return true;
	}

	internal static bool TryStageSlaughterTargets(TroopRoster requestedRoster, out int stagedCount)
	{
		stagedCount = CountRegular(requestedRoster);
		if (stagedCount <= 0)
		{
			return false;
		}
		_activeSlaughterTargets ??= TroopRoster.CreateDummyTroopRoster();
		AddRoster(_activeSlaughterTargets, requestedRoster);
		_lastRegularDispositionAction = SiegeCastleActionKind.SlaughterPrisoners;
		return true;
	}

	internal static void RollbackSlaughterTargets(TroopRoster requestedRoster)
	{
		SubtractRoster(_activeSlaughterTargets, requestedRoster);
		if ((_activeSlaughterTargets?.TotalManCount ?? 0) <= 0)
		{
			_activeSlaughterTargets = null;
		}
	}

	internal static void RecordSlaughterDeath(CharacterObject character)
	{
		if (character == null || _activeSlaughterTargets == null)
		{
			return;
		}
		int index = _activeSlaughterTargets.FindIndexOfTroop(character);
		if (index >= 0 && _activeSlaughterTargets.GetElementCopyAtIndex(index).Number > 0)
		{
			TroopRosterElement current = _activeSlaughterTargets.GetElementCopyAtIndex(index);
			int wounded = SiegeCastlePrisonerDispositionProfile.ResolveTransferredWounded(
				current.Number,
				current.WoundedNumber,
				1);
			int xp = SiegeCastlePrisonerDispositionProfile.ResolveTransferredXp(
				current.Number,
				current.Xp,
				1);
			_activeSlaughterTargets.AddToCounts(character, -1, false, -wounded, -xp, true, -1);
		}
		if (_activeSlaughterTargets.TotalManCount <= 0)
		{
			_activeSlaughterTargets = null;
		}
	}

	internal static IReadOnlyList<CastleAftermathRegularDispositionAllocation> GetDeferredAllocations()
	{
		return RegularAllocationOrder
			.Where(RegularDeferredAllocations.ContainsKey)
			.Select(action => new CastleAftermathRegularDispositionAllocation(
				action,
				CloneRegularRoster(RegularDeferredAllocations[action])))
			.ToArray();
	}

	internal static void RecordFinalizedRegularOutcome(SiegeCastleActionKind action, int affectedCount, int gold)
	{
		if (affectedCount <= 0)
		{
			return;
		}
		SiegeCastleDispositionSummaryEntry existing = FinalizedRegularOutcomes.FirstOrDefault(entry => entry.Action == action);
		if (existing != null)
		{
			int index = FinalizedRegularOutcomes.IndexOf(existing);
			FinalizedRegularOutcomes[index] = new SiegeCastleDispositionSummaryEntry(
				action,
				existing.AffectedCount + affectedCount,
				existing.Gold + Math.Max(0, gold));
			return;
		}
		FinalizedRegularOutcomes.Add(new SiegeCastleDispositionSummaryEntry(action, affectedCount, gold));
	}

	internal static IReadOnlyList<SiegeCastleDispositionSummaryEntry> GetFinalizedRegularOutcomes()
		=> FinalizedRegularOutcomes.ToArray();

	internal static string BuildRegularPlanSummary()
	{
		var parts = new List<string>();
		foreach (SiegeCastleActionKind action in RegularAllocationOrder)
		{
			if (RegularDeferredAllocations.TryGetValue(action, out TroopRoster roster) && roster.TotalManCount > 0)
			{
				parts.Add(SiegeCastleRegularDispositionStagingProfile.Describe(action) + " " + roster.TotalManCount + " 人");
			}
		}
		if ((_activeSlaughterTargets?.TotalManCount ?? 0) > 0)
		{
			parts.Add("现场屠戮目标 " + _activeSlaughterTargets.TotalManCount + " 人");
		}
		return parts.Count == 0 ? "尚未分配" : string.Join("；", parts);
	}

	internal static void UnmarkApplied(
		SiegeCastleActionKind action,
		SiegeCastleActionSpeakerRole role,
		Agent agent,
		Hero hero)
	{
		if (role == SiegeCastleActionSpeakerRole.CapturedLord)
		{
			string key = ResolveLordKey(agent, hero);
			if (!string.IsNullOrWhiteSpace(key) && LordAppliedActions.TryGetValue(key, out HashSet<SiegeCastleActionKind> actions))
			{
				actions.Remove(action);
			}
			if (!string.IsNullOrWhiteSpace(key)
				&& LordTerminalActions.TryGetValue(key, out SiegeCastleActionKind terminal)
				&& terminal == action)
			{
				LordTerminalActions.Remove(key);
			}
			return;
		}
		RegularProcessActions.Remove(action);
	}

	private static string ResolveLordKey(Agent agent, Hero hero)
	{
		hero ??= (agent?.Character as CharacterObject)?.HeroObject;
		if (!string.IsNullOrWhiteSpace(hero?.StringId))
		{
			return "hero:" + hero.StringId;
		}
		return agent != null && agent.Index >= 0 ? "agent:" + agent.Index : string.Empty;
	}

	private static TroopRoster CloneRegularRoster(TroopRoster source)
	{
		TroopRoster result = TroopRoster.CreateDummyTroopRoster();
		if (source == null)
		{
			return result;
		}
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			if (element.Character == null || element.Character.IsHero || element.Number <= 0)
			{
				continue;
			}
			result.AddToCounts(element.Character, element.Number, false, element.WoundedNumber, element.Xp, true, -1);
		}
		return result;
	}

	private static void AddRoster(TroopRoster target, TroopRoster source)
	{
		if (target == null || source == null)
		{
			return;
		}
		foreach (TroopRosterElement element in source.GetTroopRoster())
		{
			if (element.Character != null && !element.Character.IsHero && element.Number > 0)
			{
				target.AddToCounts(element.Character, element.Number, false, element.WoundedNumber, element.Xp, true, -1);
			}
		}
	}

	private static void SubtractRoster(TroopRoster target, TroopRoster source)
	{
		if (target == null || source == null)
		{
			return;
		}
		foreach (TroopRosterElement element in source.GetTroopRoster().ToList())
		{
			CharacterObject character = element.Character;
			int index = character != null ? target.FindIndexOfTroop(character) : -1;
			if (index < 0)
			{
				continue;
			}
			TroopRosterElement current = target.GetElementCopyAtIndex(index);
			int count = Math.Min(Math.Max(0, element.Number), Math.Max(0, current.Number));
			if (count > 0)
			{
				int wounded = SiegeCastlePrisonerDispositionProfile.ResolveTransferredWounded(
					element.Number,
					element.WoundedNumber,
					count);
				int xp = SiegeCastlePrisonerDispositionProfile.ResolveTransferredXp(
					element.Number,
					element.Xp,
					count);
				target.AddToCounts(character, -count, false, -wounded, -xp, true, -1);
			}
		}
	}

	private static int CountRegular(TroopRoster roster)
	{
		return roster?.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.Sum(element => element.Number) ?? 0;
	}
}

internal sealed class CastleAftermathRegularDispositionAllocation
{
	internal CastleAftermathRegularDispositionAllocation(SiegeCastleActionKind action, TroopRoster roster)
	{
		Action = action;
		Roster = roster;
	}

	internal SiegeCastleActionKind Action { get; }

	internal TroopRoster Roster { get; }
}
