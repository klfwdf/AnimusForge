using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade.ComponentInterfaces;

public abstract class MissionShipParametersModel : MBGameModel<MissionShipParametersModel>
{
	public abstract int CalculateMainDeckCrewSize(IShipOrigin shipOrigin, Agent formationUnit);

	public abstract float CalculateWindBonus(IShipOrigin shipOrigin, Agent captain, float baseSailForceMagnitude);

	public abstract float CalculateOarForceMultiplier(Agent pilotAgent, float baseOarForce);
}
