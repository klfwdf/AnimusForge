using System.Collections.Generic;
using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.WoundedBeast;

internal class FinishOffConsortsObjective : MissionObjective
{
	private class ShipObjectiveTarget : MissionObjectiveTarget
	{
		public readonly MissionShip TargetShip;

		private readonly TextObject _name;

		public ShipObjectiveTarget(MissionShip targetShip, TextObject name)
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
			if (TargetShip != null && !TargetShip.IsDisabled && !TargetShip.IsSinking)
			{
				return TargetShip.Formation.CountOfUnits > 0;
			}
			return false;
		}
	}

	private MissionObjectiveProgressInfo _cachedProgress;

	private List<MissionShip> _targets;

	public override string UniqueId => "naval_storyline_quest_2_sink_ship_objective";

	public override TextObject Name => new TextObject("{=seOnzgCs}Defeat Fahda's consorts");

	public override TextObject Description => new TextObject("{=3lZywscl}Fahda's flagship is going down. Defeat the rest of her fleet.");

	public FinishOffConsortsObjective(Mission mission, List<MissionShip> targetShips)
		: base(mission)
	{
		_targets = targetShips;
		foreach (MissionShip target in _targets)
		{
			AddTarget(new ShipObjectiveTarget(target, new TextObject("{=UaWgrVnN}Fahda's Consort")));
		}
		_cachedProgress.RequiredProgressAmount = targetShips.Count;
	}

	public override MissionObjectiveProgressInfo GetCurrentProgress()
	{
		return _cachedProgress;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		int num = 0;
		foreach (MissionShip target in _targets)
		{
			if (target.Formation.CountOfUnits == 0)
			{
				num++;
			}
		}
		_cachedProgress.CurrentProgressAmount = num;
	}

	protected override bool IsActivationRequirementsMet()
	{
		return true;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return false;
	}
}
