using System.Linq;
using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class FaceGenHotkeyCategory : GameKeyContext
{
	public const string CategoryId = "FaceGenHotkeyCategory";

	public const string Zoom = "Zoom";

	public const string Rotate = "Rotate";

	public const string Ascend = "Ascend";

	public const string ControllerRotationAxis = "CameraAxisX";

	public const string ControllerCameraUpDownAxis = "CameraAxisY";

	public const string Copy = "Copy";

	public const string Paste = "Paste";

	public const int ControllerZoomIn = 56;

	public const int ControllerZoomOut = 57;

	public FaceGenHotkeyCategory()
		: base("FaceGenHotkeyCategory", 116)
	{
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("Ascend", "FaceGenHotkeyCategory", InputKey.MiddleMouseButton));
		RegisterHotKey(new HotKey("Rotate", "FaceGenHotkeyCategory", InputKey.LeftMouseButton));
		RegisterHotKey(new HotKey("Zoom", "FaceGenHotkeyCategory", InputKey.RightMouseButton));
		RegisterHotKey(new HotKey("Copy", "FaceGenHotkeyCategory", InputKey.C));
		RegisterHotKey(new HotKey("Paste", "FaceGenHotkeyCategory", InputKey.V));
	}

	private void RegisterGameKeys()
	{
		RegisterGameKey(new GameKey(56, "ControllerZoomIn", "FaceGenHotkeyCategory", InputKey.Invalid, InputKey.ControllerRTrigger));
		RegisterGameKey(new GameKey(57, "ControllerZoomOut", "FaceGenHotkeyCategory", InputKey.Invalid, InputKey.ControllerLTrigger));
	}

	private void RegisterGameAxisKeys()
	{
		GameAxisKey gameKey = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisX"));
		GameAxisKey gameKey2 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisY"));
		RegisterGameAxisKey(gameKey);
		RegisterGameAxisKey(gameKey2);
	}
}
