using TaleWorlds.CampaignSystem;

namespace AnimusForge;

internal static class MeetingBattleRuntime
{
	private static bool _meetingActive;

	private static bool _combatEscalated;

	private static bool _diplomaticSideEffectsUnlocked;

	private static bool _combatEscalationConsequencesApplied;

	private static Hero _targetHero;

	internal static bool IsMeetingActive => _meetingActive;

	internal static bool IsCombatEscalated => _combatEscalated;

	internal static Hero TargetHero => _targetHero;

	internal static bool ShouldBlockDiplomaticSideEffects => _meetingActive && !_diplomaticSideEffectsUnlocked;

	internal static void BeginMeeting(Hero target)
	{
		_meetingActive = true;
		_combatEscalated = false;
		_diplomaticSideEffectsUnlocked = false;
		_combatEscalationConsequencesApplied = false;
		_targetHero = target;
		Logger.Log("MeetingBattle", $"Meeting session started. Target={target?.Name}");
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
