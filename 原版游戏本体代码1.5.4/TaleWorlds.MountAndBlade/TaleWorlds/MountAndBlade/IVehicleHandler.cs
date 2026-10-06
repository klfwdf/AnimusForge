using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public interface IVehicleHandler : IMissionBehavior
{
	bool IsAgentInVehicle(Agent agent, out WeakGameEntity vehicleEntity);
}
