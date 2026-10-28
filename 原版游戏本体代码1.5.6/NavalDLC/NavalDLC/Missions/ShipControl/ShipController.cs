using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipInput;

namespace NavalDLC.Missions.ShipControl;

public abstract class ShipController
{
	protected MissionShip _ownerShip;

	protected ShipControllerType _controllerType;

	public bool IsPlayerControlled => _controllerType == ShipControllerType.Player;

	public bool IsAIControlled => _controllerType == ShipControllerType.AI;

	public ShipControllerType ControllerType => _controllerType;

	public ShipController(MissionShip ownerShip)
	{
		_ownerShip = ownerShip;
		_controllerType = ShipControllerType.None;
	}

	public abstract ShipInputRecord Update(float dt);

	public virtual void Deallocate()
	{
		_ownerShip = null;
		_controllerType = ShipControllerType.None;
	}
}
