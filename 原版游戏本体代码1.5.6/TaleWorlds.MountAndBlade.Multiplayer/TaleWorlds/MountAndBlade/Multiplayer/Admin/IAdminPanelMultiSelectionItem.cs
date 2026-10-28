namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelMultiSelectionItem
{
	string Value { get; }

	string DisplayName { get; }

	bool IsFallbackValue { get; }

	bool IsDisabled { get; }

	bool CanBeApplied { get; }
}
