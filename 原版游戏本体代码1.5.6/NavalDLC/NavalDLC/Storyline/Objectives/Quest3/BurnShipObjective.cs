using NavalDLC.Missions.Objects;
using NavalDLC.Storyline.MissionControllers;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class BurnShipObjective : MissionObjective
{
	private BlockedEstuaryMissionController _controller;

	private MissionShip _targetShip;

	public override string UniqueId => "naval_storyline_quest_3_burn_ship_objective";

	public override TextObject Name => new TextObject("{=Ry0xZCO2}Ram Enemy Ship");

	public override TextObject Description => new TextObject("{=BHR7DWsG}Destroy the enemy ship by ramming it with your fireship.");

	internal BurnShipObjective(Mission mission, MissionShip targetShip)
		: base(mission)
	{
		_controller = base.Mission.GetMissionBehavior<BlockedEstuaryMissionController>();
		_targetShip = targetShip;
		AddTarget(new ShipObjectiveTarget(_targetShip, new TextObject("{=EBLRhSsY}Target Ship")));
	}

	protected override bool IsActivationRequirementsMet()
	{
		return _targetShip != null;
	}

	protected override bool IsCompletionRequirementsMet()
	{
		return _controller.ShipsCollided;
	}
}
