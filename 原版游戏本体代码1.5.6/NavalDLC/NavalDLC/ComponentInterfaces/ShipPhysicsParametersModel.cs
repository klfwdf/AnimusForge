using TaleWorlds.Core;

namespace NavalDLC.ComponentInterfaces;

public abstract class ShipPhysicsParametersModel : MBGameModel<ShipPhysicsParametersModel>
{
	public abstract float GetWaterDensity();

	public abstract float GetAirDensity();
}
