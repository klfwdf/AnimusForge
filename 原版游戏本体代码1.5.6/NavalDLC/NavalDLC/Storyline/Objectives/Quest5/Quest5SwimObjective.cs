using System.Linq;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5SwimObjective : MissionObjective
{
	private class SwimObjectiveTarget : MissionObjectiveTarget
	{
		private readonly MissionShip _target;

		public SwimObjectiveTarget(MissionShip target)
		{
			_target = target;
		}

		public override TextObject GetName()
		{
			return new TextObject("{=4hW7wMrj}Prisoner ship");
		}

		public override Vec3 GetGlobalPosition()
		{
			return _target.ClimbingMachines.First().GameEntity.GlobalPosition + Vec3.Up;
		}

		public override bool IsActive()
		{
			return true;
		}
	}

	private SwimObjectiveTarget _target;

	private MissionShip _targetShip;

	public override string UniqueId => "quest_5_swim_objective";

	public override TextObject Name => new TextObject("{=zcQhNQ7i}Reach the prisoner ship");

	public override TextObject Description => new TextObject("{=lXv922C6}Swim with Gunnar to the ship where the captives are held.");

	public Quest5SwimObjective(Mission mission, Agent targetAgent, MissionShip targetShip)
		: base(mission)
	{
		_targetShip = targetShip;
		_target = new SwimObjectiveTarget(targetShip);
		AddTarget(_target);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _target != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (_target != null)
		{
			Agent main = Agent.Main;
			if (main != null && main.IsActive())
			{
				return _targetShip.GetIsAgentOnShip(Agent.Main);
			}
		}
		return false;
	}
}
