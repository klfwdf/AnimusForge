using System.Collections.Generic;
using System.Linq;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class MeetingBattleRuntime
{
	private static bool _meetingActive;

	private static bool _combatEscalated;

	private static bool _diplomaticSideEffectsUnlocked;

	private static bool _combatEscalationConsequencesApplied;

	private static Hero _targetHero;

	private static List<Hero> _targetHeroes = new List<Hero>();

	internal static bool IsMeetingActive => _meetingActive;

	internal static bool IsCombatEscalated => _combatEscalated;

	internal static Hero TargetHero
	{
		get
		{
			if (_targetHeroes != null && _targetHeroes.Count > 0)
			{
				return _targetHeroes[0] ?? _targetHero;
			}
			return _targetHero;
		}
	}

	internal static IReadOnlyList<Hero> TargetHeroes
	{
		get
		{
			if (_targetHeroes == null || _targetHeroes.Count == 0)
			{
				if (_targetHero != null)
				{
					return new List<Hero> { _targetHero };
				}
				return new List<Hero>();
			}
			return _targetHeroes.AsReadOnly();
		}
	}

	internal static IReadOnlyList<Hero> SecondaryTargets
	{
		get
		{
			List<Hero> list = new List<Hero>();
			if (_targetHeroes != null && _targetHeroes.Count > 1)
			{
				Hero primary = _targetHeroes[0];
				for (int i = 1; i < _targetHeroes.Count; i++)
				{
					Hero h = _targetHeroes[i];
					if (h != null && h != primary && h != Hero.MainHero)
					{
						list.Add(h);
					}
				}
			}
			return list;
		}
	}

	internal static bool IsSecondaryTarget(Hero hero)
	{
		if (hero == null || _targetHeroes == null || _targetHeroes.Count < 2)
		{
			return false;
		}
		Hero primary = _targetHeroes[0];
		return hero != primary && hero != Hero.MainHero && _targetHeroes.Contains(hero);
	}

	internal static bool ShouldBlockDiplomaticSideEffects => _meetingActive && !_diplomaticSideEffectsUnlocked;

	internal static void BeginMeeting(Hero target)
	{
		BeginMeeting(new List<Hero> { target });
	}

	internal static void BeginMeeting(List<Hero> targets)
	{
		_meetingActive = true;
		_combatEscalated = false;
		_diplomaticSideEffectsUnlocked = false;
		_combatEscalationConsequencesApplied = false;
		_targetHeroes = new List<Hero>();
		_targetHero = null;
		if (targets != null)
		{
			foreach (Hero hero in targets)
			{
				if (hero != null && hero != Hero.MainHero && hero.IsLord && !_targetHeroes.Contains(hero))
				{
					_targetHeroes.Add(hero);
				}
			}
		}
		if (_targetHeroes.Count > 0)
		{
			_targetHero = _targetHeroes[0];
		}
		Logger.Log("MeetingBattle", $"Meeting session started. Targets={_targetHeroes.Count}, Names={string.Join(", ", _targetHeroes.Select((Hero h) => (h?.Name != null) ? h.Name.ToString() : "null"))}");
	}

	internal static void EndMeeting()
	{
		if (_meetingActive || _targetHero != null || _combatEscalated || _diplomaticSideEffectsUnlocked)
		{
			Logger.Log("MeetingBattle", "Meeting session ended. Reset battle/runtime guard state.");
		}
		_meetingActive = false;
		_combatEscalated = false;
		_diplomaticSideEffectsUnlocked = false;
		_combatEscalationConsequencesApplied = false;
		_targetHero = null;
		_targetHeroes = new List<Hero>();
	}

	internal static void RequestCombatEscalation(string reason)
	{
		if (!_meetingActive)
		{
			Logger.Log("MeetingBattle", "RequestCombatEscalation ignored because meeting is inactive. Reason=" + (reason ?? "N/A"));
			return;
		}
		_combatEscalated = true;
		_diplomaticSideEffectsUnlocked = true;
		try
		{
			LordEncounterBehavior.SuspendEncounterRedirectDuringResultResolution(reason);
		}
		catch
		{
		}
		Logger.Log("MeetingBattle", "Combat escalation requested. Side effects unlocked. Reason=" + (reason ?? "N/A"));
	}

	internal static void UnlockDiplomaticSideEffects(string reason)
	{
		if (_meetingActive)
		{
			_diplomaticSideEffectsUnlocked = true;
			Logger.Log("MeetingBattle", "Diplomatic side effects unlocked. Reason=" + (reason ?? "N/A"));
		}
	}

	internal static bool TryMarkCombatEscalationConsequencesApplied()
	{
		if (!_meetingActive)
		{
			return false;
		}
		if (_combatEscalationConsequencesApplied)
		{
			return false;
		}
		_combatEscalationConsequencesApplied = true;
		return true;
	}
}
