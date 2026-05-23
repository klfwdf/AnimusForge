using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class MultiplayerHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "MultiplayerHotkeyCategory";

	private const string _storeCameraPositionBase = "StoreCameraPosition";

	public const string StoreCameraPosition1 = "StoreCameraPosition1";

	public const string StoreCameraPosition2 = "StoreCameraPosition2";

	public const string StoreCameraPosition3 = "StoreCameraPosition3";

	public const string StoreCameraPosition4 = "StoreCameraPosition4";

	public const string StoreCameraPosition5 = "StoreCameraPosition5";

	public const string StoreCameraPosition6 = "StoreCameraPosition6";

	public const string StoreCameraPosition7 = "StoreCameraPosition7";

	public const string StoreCameraPosition8 = "StoreCameraPosition8";

	public const string StoreCameraPosition9 = "StoreCameraPosition9";

	private const string _spectateCameraPositionBase = "SpectateCameraPosition";

	public const string SpectateCameraPosition1 = "SpectateCameraPosition1";

	public const string SpectateCameraPosition2 = "SpectateCameraPosition2";

	public const string SpectateCameraPosition3 = "SpectateCameraPosition3";

	public const string SpectateCameraPosition4 = "SpectateCameraPosition4";

	public const string SpectateCameraPosition5 = "SpectateCameraPosition5";

	public const string SpectateCameraPosition6 = "SpectateCameraPosition6";

	public const string SpectateCameraPosition7 = "SpectateCameraPosition7";

	public const string SpectateCameraPosition8 = "SpectateCameraPosition8";

	public const string SpectateCameraPosition9 = "SpectateCameraPosition9";

	public const string InspectBadgeProgression = "InspectBadgeProgression";

	public const string PerformActionOnCosmeticItem = "PerformActionOnCosmeticItem";

	public const string PreviewCosmeticItem = "PreviewCosmeticItem";

	public const string ToggleFriendsList = "ToggleFriendsList";

	public MultiplayerHotkeyCategory()
		: base("MultiplayerHotkeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		for (int i = 1; i <= 9; i++)
		{
			RegisterHotKey(new HotKey("StoreCameraPosition" + i, "MultiplayerHotkeyCategory", (InputKey)(11 + i)));
		}
		for (int j = 1; j <= 9; j++)
		{
			RegisterHotKey(new HotKey("SpectateCameraPosition" + j, "MultiplayerHotkeyCategory", (InputKey)(11 + j)));
		}
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.RightMouseButton),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys2 = new List<Key>
		{
			new Key(InputKey.LeftMouseButton),
			new Key(InputKey.ControllerRDown)
		};
		List<Key> keys3 = new List<Key>
		{
			new Key(InputKey.RightMouseButton),
			new Key(InputKey.ControllerRUp)
		};
		List<Key> keys4 = new List<Key>
		{
			new Key(InputKey.F),
			new Key(InputKey.ControllerRLeft)
		};
		RegisterHotKey(new HotKey("PerformActionOnCosmeticItem", "MultiplayerHotkeyCategory", keys2));
		RegisterHotKey(new HotKey("PreviewCosmeticItem", "MultiplayerHotkeyCategory", keys3));
		RegisterHotKey(new HotKey("InspectBadgeProgression", "MultiplayerHotkeyCategory", keys));
		RegisterHotKey(new HotKey("ToggleFriendsList", "MultiplayerHotkeyCategory", keys4));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
