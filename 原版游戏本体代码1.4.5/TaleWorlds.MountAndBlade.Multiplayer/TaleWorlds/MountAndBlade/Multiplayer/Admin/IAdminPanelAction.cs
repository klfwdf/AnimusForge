namespace TaleWorlds.MountAndBlade.Multiplayer.Admin;

public interface IAdminPanelAction
{
	string UniqueId { get; }

	string Name { get; }

	string Description { get; }

	bool GetIsDisabled(out string reason);

	void OnActionExecuted();

	bool GetIsAvailable();
}
