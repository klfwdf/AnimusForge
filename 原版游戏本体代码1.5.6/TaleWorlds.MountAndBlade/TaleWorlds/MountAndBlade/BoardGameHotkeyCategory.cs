using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class BoardGameHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "BoardGameHotkeyCategory";

	public const string BoardGamePawnSelect = "BoardGamePawnSelect";

	public const string BoardGamePawnDeselect = "BoardGamePawnDeselect";

	public const string BoardGameDragPreview = "BoardGameDragPreview";

	public const string BoardGameRollDice = "BoardGameRollDice";

	public BoardGameHotkeyCategory()
		: base("BoardGameHotkeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		List<Key> keys4 = new List<Key>
		{
			new Key(InputKey.Space),
			new Key(InputKey.ControllerRBumper)
		};
		RegisterHotKey(new HotKey("BoardGamePawnSelect", "BoardGameHotkeyCategory", keys));
		RegisterHotKey(new HotKey("BoardGamePawnDeselect", "BoardGameHotkeyCategory", keys2));
		RegisterHotKey(new HotKey("BoardGameDragPreview", "BoardGameHotkeyCategory", keys3));
		RegisterHotKey(new HotKey("BoardGameRollDice", "BoardGameHotkeyCategory", keys4));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
