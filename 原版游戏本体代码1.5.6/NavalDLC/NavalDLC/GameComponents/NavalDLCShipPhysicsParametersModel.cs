using NavalDLC.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalDLCShipPhysicsParametersModel : ShipPhysicsParametersModel
{
	public override float GetWaterDensity()
	{
		return 1020f;
	}

	public override float GetAirDensity()
	{
		return 1.225f;
	}
}
