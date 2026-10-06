using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelOptionGroup
{
	string UniqueId { get; }

	bool RequiresRestart { get; }

	TextObject Name { get; }

	MBReadOnlyList<IAdminPanelOption> Options { get; }

	MBReadOnlyList<IAdminPanelAction> Actions { get; }

	void OnFinalize();
}
