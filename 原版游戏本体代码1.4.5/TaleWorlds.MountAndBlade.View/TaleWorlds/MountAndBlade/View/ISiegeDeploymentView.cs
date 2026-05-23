using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.View;

public interface ISiegeDeploymentView
{
	void OnEntitySelection(WeakGameEntity selectedEntity);

	void OnEntityHover(WeakGameEntity hoveredEntity);
}
