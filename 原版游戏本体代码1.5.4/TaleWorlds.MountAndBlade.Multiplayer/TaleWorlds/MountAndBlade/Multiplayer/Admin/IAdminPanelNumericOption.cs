namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelNumericOption : IAdminPanelOption<int>, IAdminPanelOption
{
	int? GetMinimumValue();

	int? GetMaximumValue();
}
