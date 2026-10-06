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

	public const string CycleSpectatorCamera = "CycleSpectatorCamera";

	public const string CycleSpectatorTargetPrevious = "CycleSpectatorTargetPrevious";

	public const string CycleSpectatorTargetNext = "CycleSpectatorTargetNext";

	public const string InspectBadgeProgression = "InspectBadgeProgression";

	public const string PerformActionOnCosmeticItem = "PerformActionOnCosmeticItem";

	public const string PreviewCosmeticItem = "PreviewCosmeticItem";

	public const string ToggleFriendsList = "ToggleFriendsList";

	private static readonly InputKey[] CameraPositionDigitKeys = new InputKey[9]
	{
		InputKey.D1,
		InputKey.D2,
		InputKey.D3,
		InputKey.D4,
		InputKey.D5,
		InputKey.D6,
		InputKey.D7,
		InputKey.D8,
		InputKey.D9
	};

	public static readonly string[] StoreCameraPositionHotKeys = new string[9] { "StoreCameraPosition1", "StoreCameraPosition2", "StoreCameraPosition3", "StoreCameraPosition4", "StoreCameraPosition5", "StoreCameraPosition6", "StoreCameraPosition7", "StoreCameraPosition8", "StoreCameraPosition9" };

	public static readonly string[] SpectateCameraPositionHotKeys = new string[9] { "SpectateCameraPosition1", "SpectateCameraPosition2", "SpectateCameraPosition3", "SpectateCameraPosition4", "SpectateCameraPosition5", "SpectateCameraPosition6", "SpectateCameraPosition7", "SpectateCameraPosition8", "SpectateCameraPosition9" };

	public MultiplayerHotkeyCategory()
		: base("MultiplayerHotkeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		for (int i = 0; i < CameraPositionDigitKeys.Length; i++)
		{
			RegisterHotKey(new HotKey("StoreCameraPosition" + (i + 1), "MultiplayerHotkeyCategory", CameraPositionDigitKeys[i]));
		}
		for (int j = 0; j < CameraPositionDigitKeys.Length; j++)
		{
			RegisterHotKey(new HotKey("SpectateCameraPosition" + (j + 1), "MultiplayerHotkeyCategory", CameraPositionDigitKeys[j]));
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
		List<Key> keys5 = new List<Key>
		{
			new Key(InputKey.V),
			new Key(InputKey.ControllerRRight)
		};
		RegisterHotKey(new HotKey("PerformActionOnCosmeticItem", "MultiplayerHotkeyCategory", keys2));
		RegisterHotKey(new HotKey("PreviewCosmeticItem", "MultiplayerHotkeyCategory", keys3));
		RegisterHotKey(new HotKey("InspectBadgeProgression", "MultiplayerHotkeyCategory", keys));
		RegisterHotKey(new HotKey("ToggleFriendsList", "MultiplayerHotkeyCategory", keys4));
		RegisterHotKey(new HotKey("CycleSpectatorCamera", "MultiplayerHotkeyCategory", keys5));
		List<Key> keys6 = new List<Key>
		{
			new Key(InputKey.Q),
			new Key(InputKey.ControllerLBumper)
		};
		RegisterHotKey(new HotKey("CycleSpectatorTargetPrevious", "MultiplayerHotkeyCategory", keys6));
		List<Key> keys7 = new List<Key>
		{
			new Key(InputKey.E),
			new Key(InputKey.ControllerRBumper)
		};
		RegisterHotKey(new HotKey("CycleSpectatorTargetNext", "MultiplayerHotkeyCategory", keys7));
	}

	private void RegisterGameKeys()
	{
	}

	private void RegisterGameAxisKeys()
	{
	}
}
