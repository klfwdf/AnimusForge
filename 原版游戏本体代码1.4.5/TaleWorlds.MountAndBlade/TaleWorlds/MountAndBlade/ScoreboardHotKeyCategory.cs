using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class ScoreboardHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "ScoreboardHotKeyCategory";

	public const int ShowMouse = 35;

	public const string HoldShow = "HoldShow";

	public const string ToggleFastForward = "ToggleFastForward";

	public const string TogglePause = "TogglePause";

	public const string MenuShowContextMenu = "MenuShowContextMenu";

	public ScoreboardHotKeyCategory()
		: base("ScoreboardHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.F),
			new Key(InputKey.ControllerRUp)
		};
		RegisterHotKey(new HotKey("ToggleFastForward", "ScoreboardHotKeyCategory", keys));
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.Escape),
			new Key(InputKey.ControllerROption)
		};
		RegisterHotKey(new HotKey("TogglePause", "ScoreboardHotKeyCategory", keys2));
		RegisterHotKey(new HotKey("MenuShowContextMenu", "ScoreboardHotKeyCategory", InputKey.RightMouseButton));
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.Tab),
			new Key(InputKey.ControllerRRight)
		};
		RegisterHotKey(new HotKey("HoldShow", "ScoreboardHotKeyCategory", keys3));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(35, "ShowMouse", "ScoreboardHotKeyCategory", InputKey.MiddleMouseButton, InputKey.ControllerLThumb, GameKeyMainCategories.ActionCategory));
	}

	private void RegisterGameAxisKeys()
	{
	}
}
