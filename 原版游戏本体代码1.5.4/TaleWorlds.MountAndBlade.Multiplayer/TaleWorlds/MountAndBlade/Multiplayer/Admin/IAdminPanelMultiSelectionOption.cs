using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelMultiSelectionOption : IAdminPanelOption<IAdminPanelMultiSelectionItem>, IAdminPanelOption
{
	MBReadOnlyList<IAdminPanelMultiSelectionItem> GetAvailableOptions();
}
