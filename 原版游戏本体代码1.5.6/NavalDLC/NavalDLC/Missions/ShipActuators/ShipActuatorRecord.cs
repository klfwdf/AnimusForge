namespace NavalDLC.Missions.ShipActuators;

public struct ShipActuatorRecord(float rowerThrust, float rowerThrustDoubleTap, float rowerRotation, float rudderRotation, float squareSailSetting, float lateenSailSetting)
{
	public readonly float RowerThrust = rowerThrust;

	public readonly float RowerThrustDoubleTap = rowerThrustDoubleTap;

	public readonly float RowerRotation = rowerRotation;

	public readonly float RudderRotation = rudderRotation;

	public readonly float SquareSailSetting = squareSailSetting;

	public readonly float LateenSailSetting = lateenSailSetting;
}
