using System.Collections.Generic;
using AnimusForge.ExpeditionParade.Mission;
using AnimusForge.ExpeditionParade.Routing;

namespace AnimusForge.ExpeditionParade.Presentation;

internal interface IParadeCameraController
{
	void Begin(ParadeCoordinator coordinator);

	void End();
}

internal interface IParadeDebugOverlay
{
	void ShowRoute(ParadeRoutePlan route);

	void UpdateFormations(IReadOnlyList<ParadeFormationController> formations);

	void ShowFailure(string code, string detail);

	void Hide();
}

internal sealed class NullParadeCameraController : IParadeCameraController
{
	public void Begin(ParadeCoordinator coordinator)
	{
	}

	public void End()
	{
	}
}

internal sealed class NullParadeDebugOverlay : IParadeDebugOverlay
{
	public void ShowRoute(ParadeRoutePlan route)
	{
	}

	public void UpdateFormations(IReadOnlyList<ParadeFormationController> formations)
	{
	}

	public void ShowFailure(string code, string detail)
	{
	}

	public void Hide()
	{
	}
}
