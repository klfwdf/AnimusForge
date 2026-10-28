using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5CheckInteriorObjective : MissionObjective
{
	private class CheckInteriorObjectiveTarget : MissionObjectiveTarget<GameEntity>
	{
		public CheckInteriorObjectiveTarget(GameEntity target)
			: base(target)
		{
		}

		public override TextObject GetName()
		{
			return new TextObject("{=shipHold}Hold");
		}

		public override Vec3 GetGlobalPosition()
		{
			return base.Target.GetGlobalFrame().origin + Vec3.Up;
		}

		public override bool IsActive()
		{
			return true;
		}
	}

	private readonly GameEntity _interiorSpawnPointEntity;

	private CheckInteriorObjectiveTarget _targetDoor;

	public override string UniqueId => "quest_5_check_interior_objective";

	public override TextObject Name => new TextObject("{=eVJ4HNv1}Enter the hold");

	public override TextObject Description => new TextObject("{=aKzRozvo}Enter the hold of the ship.");

	public Quest5CheckInteriorObjective(Mission mission, GameEntity targetDoor, GameEntity interiorSpawnPointEntity)
		: base(mission)
	{
		_interiorSpawnPointEntity = interiorSpawnPointEntity;
		_targetDoor = new CheckInteriorObjectiveTarget(targetDoor);
		AddTarget(_targetDoor);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _targetDoor != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (_targetDoor != null)
		{
			Agent main = Agent.Main;
			if (main != null && main.IsActive())
			{
				return Agent.Main.Position.Distance(_interiorSpawnPointEntity.GlobalPosition) <= 3f;
			}
		}
		return false;
	}
}
