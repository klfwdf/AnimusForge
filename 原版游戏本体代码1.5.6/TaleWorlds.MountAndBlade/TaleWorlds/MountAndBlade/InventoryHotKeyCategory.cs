using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class InventoryHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "InventoryHotKeyCategory";

	public const string SwitchAlternative = "SwitchAlternative";

	public InventoryHotKeyCategory()
		: base("InventoryHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("SwitchAlternative", "InventoryHotKeyCategory", InputKey.LeftAlt));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
