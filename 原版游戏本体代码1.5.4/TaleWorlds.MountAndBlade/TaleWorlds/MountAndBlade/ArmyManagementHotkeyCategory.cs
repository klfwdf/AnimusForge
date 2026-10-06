using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class ArmyManagementHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "ArmyManagementHotkeyCategory";

	public const string RemoveParty = "RemoveParty";

	public ArmyManagementHotkeyCategory()
		: base("ArmyManagementHotkeyCategory", 116)
	{
		RegisterHotKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("RemoveParty", "ArmyManagementHotkeyCategory", InputKey.ControllerRBumper));
	}
}
