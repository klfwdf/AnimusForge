using System.Linq;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class CraftingHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "CraftingHotkeyCategory";

	public const string Zoom = "Zoom";

	public const string Rotate = "Rotate";

	public const string Ascend = "Ascend";

	public const string ResetCamera = "ResetCamera";

	public const string Copy = "Copy";

	public const string Paste = "Paste";

	public const string ControllerRotationAxisX = "CameraAxisX";

	public const string ControllerRotationAxisY = "CameraAxisY";

	public const int ControllerZoomIn = 56;

	public const int ControllerZoomOut = 57;

	public CraftingHotkeyCategory()
		: base("CraftingHotkeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("Ascend", "CraftingHotkeyCategory", InputKey.MiddleMouseButton));
		RegisterHotKey(new HotKey("Rotate", "CraftingHotkeyCategory", InputKey.LeftMouseButton));
		RegisterHotKey(new HotKey("Zoom", "CraftingHotkeyCategory", InputKey.RightMouseButton));
		RegisterHotKey(new HotKey("Copy", "CraftingHotkeyCategory", InputKey.C));
		RegisterHotKey(new HotKey("Paste", "CraftingHotkeyCategory", InputKey.V));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(56, "ControllerZoomIn", "CraftingHotkeyCategory", InputKey.Invalid, InputKey.ControllerRTrigger));
		RegisterGameKey(new GameKey(57, "ControllerZoomOut", "CraftingHotkeyCategory", InputKey.Invalid, InputKey.ControllerLTrigger));
	}

	private void RegisterGameAxisKeys()
	{
		GameAxisKey gameKey = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisX"));
		GameAxisKey gameKey2 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisY"));
		RegisterGameAxisKey(gameKey);
		RegisterGameAxisKey(gameKey2);
	}
}
