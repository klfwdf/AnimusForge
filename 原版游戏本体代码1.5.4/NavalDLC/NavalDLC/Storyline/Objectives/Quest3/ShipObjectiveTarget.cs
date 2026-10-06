using NavalDLC.Missions.Objects;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Missions.Objectives;

namespace NavalDLC.Storyline.Objectives.Quest3;

internal class ShipObjectiveTarget : MissionObjectiveTarget
{
	private readonly MissionShip _ship;

	private readonly TextObject _name;

	private readonly bool _showController;

	internal ShipObjectiveTarget(MissionShip ship, TextObject name, bool showController = false)
	{
		_ship = ship;
		_name = name;
		_showController = showController;
	}

	public override Vec3 GetGlobalPosition()
	{
		if (_showController)
		{
			return _ship.ShipControllerMachine.GameEntity.GlobalPosition + Vec3.Up;
		}
		return _ship.GameEntity.GlobalPosition + Vec3.Up * 3f;
	}

	public override TextObject GetName()
	{
		return _name;
	}

	public override bool IsActive()
	{
		if (_ship != null && !_ship.IsDisabled)
		{
			if (_showController)
			{
				return !_ship.IsPlayerControlled;
			}
			return true;
		}
		return false;
	}
}
