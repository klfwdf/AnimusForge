using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelOptionProvider
{
	MBReadOnlyList<IAdminPanelOptionGroup> GetOptionGroups();

	IAdminPanelOption GetOptionWithId(string id);

	IAdminPanelAction GetActionWithId(string id);

	void ApplyOptions();

	void OnTick(float dt);

	void OnFinalize();
}
