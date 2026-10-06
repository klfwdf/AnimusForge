using NavalDLC.Missions.Objects.UsableMachines;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest4;

public class DestroyMangonelsObjective : MissionObjective
{
	private readonly int _initialTargets;

	private int _remainingTargets;

	public override string UniqueId => "naval_storyline_quest_4_destroy_targets_objective";

	public override TextObject Name => new TextObject("{=ZpuppygP}Destroy the Mangonels");

	public override TextObject Description => new TextObject("{=OrI07kdd}Steer the Wasp and destroy the mangonels with your ballista without getting hit yourself");

	public DestroyMangonelsObjective(Mission mission, MBList<ShipMangonel> targets)
		: base(mission)
	{
		_initialTargets = targets.Count;
		_remainingTargets = targets.Count;
		foreach (ShipMangonel target in targets)
		{
			AddTarget(new MangonelObjectiveTarget(target));
			target.DestructionComponent.OnDestroyed += OnMangonelDestroyed;
		}
	}

	private void OnMangonelDestroyed(DestructableComponent target, Agent attackerAgent, in MissionWeapon weapon, ScriptComponentBehavior attackerScriptComponentBehavior, int inflictedDamage)
	{
		_remainingTargets--;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _remainingTargets > 0;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _remainingTargets == 0;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return new MissionObjectiveProgressInfo
		{
			CurrentProgressAmount = _initialTargets - _remainingTargets,
			RequiredProgressAmount = _initialTargets
		};
	}
}
