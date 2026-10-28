using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.WoundedBeast;

internal class SinkShipObjective : MissionObjective
{
	private class SinkShipObjectiveTarget : MissionObjectiveTarget
	{
		public readonly MissionShip TargetShip;

		private readonly TextObject _name;

		public SinkShipObjectiveTarget(MissionShip targetShip, TextObject name)
		{
			TargetShip = targetShip;
			_name = name;
		}

		public override Vec3 GetGlobalPosition()
		{
			return TargetShip.GameEntity.GlobalPosition;
		}

		public override TextObject GetName()
		{
			return _name;
		}

		public override bool IsActive()
		{
			if (TargetShip != null)
			{
				return !TargetShip.IsSinking;
			}
			return false;
		}
	}

	private readonly MissionShip _targetShip;

	private SinkShipObjectiveTarget _sinkShipObjectiveTarget;

	public override string UniqueId => "naval_storyline_quest_2_sink_ship_objective";

	public override TextObject Name => new TextObject("{=VMVbnNau}Sink Fahda's Flagship");

	public override TextObject Description => new TextObject("{=rlEJ3pC8}Fahda's flagship was crippled by the storm. Ram it until it sinks!");

	public SinkShipObjective(Mission mission, MissionShip targetShip)
		: base(mission)
	{
		_targetShip = targetShip;
		_sinkShipObjectiveTarget = new SinkShipObjectiveTarget(_targetShip, new TextObject("{=gCWSOyLJ}Fahda's Ship"));
		AddTarget(_sinkShipObjectiveTarget);
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _targetShip != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		if (_targetShip != null)
		{
			if (!(_targetShip.HitPoints <= 0f))
			{
				return _targetShip.IsSinking;
			}
			return true;
		}
		return false;
	}
}
