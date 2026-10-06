using NavalDLC.Missions.Objects;
using NavalDLC.Missions.ShipActuators;
using TaleWorlds.Library;

namespace NavalDLC.Missions.ShipInput;

public class ShipInputProcessor
{
	private MissionShip _ownerShip;

	private float _rowerThrust;

	private float _rowerRotation;

	private float _rudderRotation;

	private float _squareSailSetting;

	private float _lateenSailSetting;

	public ShipInputProcessor(MissionShip ownerShip)
	{
		_ownerShip = ownerShip;
		_rowerThrust = 0f;
		_rowerRotation = 0f;
		_rudderRotation = 0f;
		_squareSailSetting = 0f;
		_lateenSailSetting = 0f;
	}

	public ShipActuatorRecord OnParallelFixedTick(float fixedDt, in ShipInputRecord inputRecord)
	{
		if (inputRecord.RowerLongitudinal == RowerLongitudinalInput.Forward)
		{
			_rowerThrust = 1f;
		}
		else if (inputRecord.RowerLongitudinal == RowerLongitudinalInput.Backward)
		{
			_rowerThrust = -1f;
		}
		else
		{
			_rowerThrust = 0f;
		}
		float rowerThrustDoubleTap = 0f;
		if (inputRecord.RowerLongitudinalDoubleTap == RowerLongitudinalInput.Forward)
		{
			rowerThrustDoubleTap = 1f;
		}
		else if (inputRecord.RowerLongitudinalDoubleTap == RowerLongitudinalInput.Backward)
		{
			rowerThrustDoubleTap = -1f;
		}
		if (inputRecord.RowerLateral == RowerLateralInput.Left)
		{
			_rowerRotation = 1f;
		}
		else if (inputRecord.RowerLateral == RowerLateralInput.Right)
		{
			_rowerRotation = -1f;
		}
		else if (inputRecord.RowerLateral == RowerLateralInput.Stop)
		{
			_rowerRotation = 0f;
		}
		else
		{
			_rowerRotation = 0f;
		}
		_rudderRotation = inputRecord.RudderLateral;
		if (inputRecord.Sail == SailInput.Raised)
		{
			_squareSailSetting = 0f;
			_lateenSailSetting = 0f;
		}
		else if (inputRecord.Sail == SailInput.SquareSailsRaised)
		{
			_squareSailSetting = 0f;
			_lateenSailSetting = 1f;
		}
		else if (inputRecord.Sail == SailInput.Full)
		{
			_squareSailSetting = 1f;
			_lateenSailSetting = 1f;
		}
		_squareSailSetting = MathF.Clamp(_squareSailSetting, 0f, 1f);
		_lateenSailSetting = MathF.Clamp(_lateenSailSetting, 0f, 1f);
		return new ShipActuatorRecord(_rowerThrust, rowerThrustDoubleTap, _rowerRotation, _rudderRotation, _squareSailSetting, _lateenSailSetting);
	}

	public void Deallocate()
	{
		_ownerShip = null;
	}
}
