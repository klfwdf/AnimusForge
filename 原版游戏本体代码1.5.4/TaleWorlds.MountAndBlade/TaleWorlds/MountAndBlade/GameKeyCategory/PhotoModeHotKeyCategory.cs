using System.Collections.Generic;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade.GameKeyCategory;

public sealed class PhotoModeHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "PhotoModeHotKeyCategory";

	public const int HideUI = 92;

	public const int CameraRollLeft = 93;

	public const int CameraRollRight = 94;

	public const int ToggleCameraFollowMode = 97;

	public const int TakePicture = 95;

	public const int TakePictureWithAdditionalPasses = 96;

	public const int ToggleMouse = 98;

	public const int ToggleVignette = 99;

	public const int ToggleCharacters = 100;

	public const int Reset = 107;

	public const string FasterCamera = "FasterCamera";

	public PhotoModeHotKeyCategory()
		: base("PhotoModeHotKeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.LeftShift),
			new Key(InputKey.ControllerRTrigger)
		};
		RegisterHotKey(new HotKey("FasterCamera", "PhotoModeHotKeyCategory", keys));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(92, "HideUI", "PhotoModeHotKeyCategory", InputKey.H, InputKey.ControllerRUp, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(93, "CameraRollLeft", "PhotoModeHotKeyCategory", InputKey.Q, InputKey.ControllerLBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(94, "CameraRollRight", "PhotoModeHotKeyCategory", InputKey.E, InputKey.ControllerRBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(97, "ToggleCameraFollowMode", "PhotoModeHotKeyCategory", InputKey.V, InputKey.ControllerRLeft, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(95, "TakePicture", "PhotoModeHotKeyCategory", InputKey.Enter, InputKey.ControllerRDown, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(96, "TakePictureWithAdditionalPasses", "PhotoModeHotKeyCategory", InputKey.BackSpace, InputKey.ControllerRBumper, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(98, "ToggleMouse", "PhotoModeHotKeyCategory", InputKey.C, InputKey.ControllerLThumb, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(99, "ToggleVignette", "PhotoModeHotKeyCategory", InputKey.X, InputKey.ControllerRThumb, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(100, "ToggleCharacters", "PhotoModeHotKeyCategory", InputKey.B, InputKey.ControllerRRight, GameKeyMainCategories.PhotoModeCategory));
		RegisterGameKey(new GameKey(107, "Reset", "PhotoModeHotKeyCategory", InputKey.T, InputKey.ControllerLOption, GameKeyMainCategories.PhotoModeCategory));
	}

	private void RegisterGameAxisKeys()
	{
	}
}
