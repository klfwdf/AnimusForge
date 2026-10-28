using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest5;

public class Quest5ReturnToDeckObjective : MissionObjective
{
	private class ReturnToDeckObjectiveTarget : MissionObjectiveTarget<GameEntity>
	{
		public ReturnToDeckObjectiveTarget(GameEntity target)
			: base(target)
		{
		}

		public override TextObject GetName()
		{
			return new TextObject("{=5MH4xtlD}Gunnar");
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

	private GameEntity _deckSpawnPointEntity;

	private ReturnToDeckObjectiveTarget _targetDoor;

	public override string UniqueId => "quest_5_return_to_deck_objective";

	public override TextObject Name => new TextObject("{=Cvwf3F6h}Return to Gunnar");

	public override TextObject Description => new TextObject("{=ZRLg1dYM}Leave the hold to talk to Gunnar.");

	public Quest5ReturnToDeckObjective(Mission mission, GameEntity targetDoorEntity, GameEntity deckSpawnPointEntity)
		: base(mission)
	{
		_deckSpawnPointEntity = deckSpawnPointEntity;
		_targetDoor = new ReturnToDeckObjectiveTarget(targetDoorEntity);
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
				return Agent.Main.Position.Distance(_deckSpawnPointEntity.GlobalPosition) <= 3f;
			}
		}
		return false;
	}
}
