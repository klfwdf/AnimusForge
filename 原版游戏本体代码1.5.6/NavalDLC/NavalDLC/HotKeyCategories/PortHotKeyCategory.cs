using System.Collections.Generic;
using System.Linq;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.HotKeyCategories;

public class PortHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "PortHotKeyCategory";

	public const string SelectLeftRoster = "SelectLeftRoster";

	public const string SelectRightRoster = "SelectRightRoster";

	public const string ToggleCameraMovement = "ToggleCameraMovement";

	public const string ResetCamera = "ResetCamera";

	public const string ControllerDeviateLeft = "ControllerDeviateLeft";

	public const string ControllerDeviateRight = "ControllerDeviateRight";

	public const string ControllerZoomIn = "ControllerZoomIn";

	public const string ControllerZoomOut = "ControllerZoomOut";

	public const string ControllerHorizontalRotationAxis = "CameraAxisX";

	public const string ControllerVerticalRotationAxis = "CameraAxisY";

	public const string CameraTargetDeviationAxis = "MovementAxisX";

	public const string ZoomAxis = "MovementAxisY";

	public PortHotKeyCategory()
		: base("PortHotKeyCategory", 0)
	{
		RegisterHotKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
		RegisterHotKey(new HotKey("SelectLeftRoster", "PortHotKeyCategory", InputKey.ControllerLTrigger));
		RegisterHotKey(new HotKey("SelectRightRoster", "PortHotKeyCategory", InputKey.ControllerRTrigger));
		RegisterHotKey(new HotKey("ControllerDeviateLeft", "PortHotKeyCategory", InputKey.ControllerLBumper));
		RegisterHotKey(new HotKey("ControllerDeviateRight", "PortHotKeyCategory", InputKey.ControllerRBumper));
		RegisterHotKey(new HotKey("ControllerZoomIn", "PortHotKeyCategory", InputKey.ControllerRTrigger));
		RegisterHotKey(new HotKey("ControllerZoomOut", "PortHotKeyCategory", InputKey.ControllerLTrigger));
		List<Key> keys = new List<Key>
		{
			new Key(InputKey.RightMouseButton),
			new Key(InputKey.ControllerLThumb)
		};
		RegisterHotKey(new HotKey("ToggleCameraMovement", "PortHotKeyCategory", keys));
		keys = new List<Key>
		{
			new Key(InputKey.R),
			new Key(InputKey.ControllerRThumb)
		};
		RegisterHotKey(new HotKey("ResetCamera", "PortHotKeyCategory", keys));
	}

	private void RegisterGameAxisKeys()
	{
		GameAxisKey gameKey = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisX"));
		GameAxisKey gameKey2 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("CameraAxisY"));
		GameAxisKey gameKey3 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("MovementAxisX"));
		GameAxisKey gameKey4 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("MovementAxisY"));
		RegisterGameAxisKey(gameKey);
		RegisterGameAxisKey(gameKey2);
		RegisterGameAxisKey(gameKey3);
		RegisterGameAxisKey(gameKey4);
	}
}
