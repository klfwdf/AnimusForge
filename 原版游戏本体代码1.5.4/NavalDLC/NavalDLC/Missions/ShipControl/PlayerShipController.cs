using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipInput;

namespace NavalDLC.Missions.ShipControl;

public class PlayerShipController : ShipController
{
	private ShipInputRecord _inputRecord;

	public PlayerShipController(MissionShip ownerShip)
		: base(ownerShip)
	{
		_controllerType = ShipControllerType.Player;
	}

	public void SetInput(in ShipInputRecord inputRecord)
	{
		_inputRecord = inputRecord;
	}

	public override ShipInputRecord Update(float dt)
	{
		return _inputRecord;
	}
}
