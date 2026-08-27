using AnimusForge.ExpeditionParade.Core;
using AnimusForge.ExpeditionParade.Routing;

namespace AnimusForge.ExpeditionParade.Mission;

internal interface IParadeMissionRuntimeAdapter
{
	ParadeRouteResolution ResolveRoute(ParadeCoordinator coordinator, TaleWorlds.MountAndBlade.Mission mission);

	ParadeOperationResult Tick(ParadeCoordinator coordinator, TaleWorlds.MountAndBlade.Mission mission, float deltaTime);

	void Cleanup(ParadeCoordinator coordinator, TaleWorlds.MountAndBlade.Mission mission);
}
