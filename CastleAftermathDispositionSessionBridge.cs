using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Mission-scoped castle disposition ledger. It is intentionally separate from town GCCZ
/// outcome flags and contains no prompt/business constants.
/// </summary>
internal static class CastleAftermathDispositionSessionBridge
{
	private static readonly HashSet<SiegeCastleActionKind> RegularAppliedActions = new HashSet<SiegeCastleActionKind>();
	private static readonly Dictionary<string, HashSet<SiegeCastleActionKind>> LordAppliedActions = new Dictionary<string, HashSet<SiegeCastleActionKind>>(StringComparer.OrdinalIgnoreCase);
	private static readonly Dictionary<string, SiegeCastleActionKind> LordTerminalActions = new Dictionary<string, SiegeCastleActionKind>(StringComparer.OrdinalIgnoreCase);
	private static readonly List<string> LordOutcomeSummaries = new List<string>();
	private static SiegeCastleActionKind _regularTerminalAction = SiegeCastleActionKind.Unknown;

	internal static void Reset(string source)
	{
		RegularAppliedActions.Clear();
		LordAppliedActions.Clear();
		LordTerminalActions.Clear();
		LordOutcomeSummaries.Clear();
		_regularTerminalAction = SiegeCastleActionKind.Unknown;
		Logger.Log("CastleAftermath", "Reset castle disposition session ledger. Source=" + (source ?? "N/A"));
	}

	internal static void RecordLordOutcome(string summary)
	{
		string normalized = (summary ?? string.Empty).Trim();
		if (normalized.Length > 0 && !LordOutcomeSummaries.Contains(normalized))
		{
			LordOutcomeSummaries.Add(normalized);
		}
	}

	internal static string GetLordOutcomeSummary()
	{
		return string.Join("；", LordOutcomeSummaries);
	}

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
		return role == SiegeCastleActionSpeakerRole.RegularPrisoner
			|| role == SiegeCastleActionSpeakerRole.AlliedSoldier
			? RegularAppliedActions.ToArray()
			: Array.Empty<SiegeCastleActionKind>();
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
			? _regularTerminalAction
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
		if (SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action))
		{
			return TryStageRegularTerminalAction(action, role, out _);
		}
		if (!RegularAppliedActions.Add(action))
		{
			return false;
		}
		return true;
	}

	internal static bool TryStageRegularTerminalAction(
		SiegeCastleActionKind action,
		SiegeCastleActionSpeakerRole role,
		out SiegeCastleActionKind previousAction)
	{
		previousAction = _regularTerminalAction;
		if (!SiegeCastleActionKindProfile.IsRegularPrisonerTerminal(action)
			|| (role != SiegeCastleActionSpeakerRole.RegularPrisoner
				&& role != SiegeCastleActionSpeakerRole.AlliedSoldier)
			|| previousAction == action)
		{
			return false;
		}

		if (previousAction != SiegeCastleActionKind.Unknown)
		{
			RegularAppliedActions.Remove(previousAction);
		}
		RegularAppliedActions.Add(action);
		_regularTerminalAction = action;
		return true;
	}

	internal static void RestoreRegularTerminalAction(
		SiegeCastleActionKind failedAction,
		SiegeCastleActionKind previousAction)
	{
		if (_regularTerminalAction == failedAction)
		{
			RegularAppliedActions.Remove(failedAction);
		}
		_regularTerminalAction = previousAction;
		if (previousAction != SiegeCastleActionKind.Unknown)
		{
			RegularAppliedActions.Add(previousAction);
		}
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
		RegularAppliedActions.Remove(action);
		if (_regularTerminalAction == action)
		{
			_regularTerminalAction = SiegeCastleActionKind.Unknown;
		}
	}

	private static string ResolveLordKey(Agent agent, Hero hero)
	{
		hero ??= (agent?.Character as TaleWorlds.CampaignSystem.CharacterObject)?.HeroObject;
		if (!string.IsNullOrWhiteSpace(hero?.StringId))
		{
			return "hero:" + hero.StringId;
		}
		return agent != null && agent.Index >= 0 ? "agent:" + agent.Index : string.Empty;
	}
}
