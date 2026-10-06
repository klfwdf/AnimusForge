using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GameKeyCategory;

public class OrderOfBattleHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "OrderOfBattleHotKeyCategory";

	public const string Confirm = "Confirm";

	public const string Exit = "Exit";

	public const string AutoDeploy = "AutoDeploy";

	public OrderOfBattleHotKeyCategory()
		: base("OrderOfBattleHotKeyCategory", 0)
	{
		RegisterHotKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.Escape),
			new Key(InputKey.ControllerRRight)
		};
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.Enter),
			new Key(InputKey.NumpadEnter),
			new Key(InputKey.ControllerRLeft)
		};
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.ControllerRUp)
		};
		RegisterHotKey(new HotKey("Exit", "OrderOfBattleHotKeyCategory", keys));
		RegisterHotKey(new HotKey("Confirm", "OrderOfBattleHotKeyCategory", keys2));
		RegisterHotKey(new HotKey("AutoDeploy", "OrderOfBattleHotKeyCategory", keys3));
	}
}
