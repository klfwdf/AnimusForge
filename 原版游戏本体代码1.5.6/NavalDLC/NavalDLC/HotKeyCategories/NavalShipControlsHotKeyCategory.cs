using System.Linq;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.HotKeyCategories;

public class NavalShipControlsHotKeyCategory : GameKeyContext
{
	public const string CategoryId = "NavalShipControlsHotKeyCategory";

	public const string AccelerationAxis = "MovementAxisY";

	public const string TurnAxis = "MovementAxisX";

	public const int ToggleSail = 110;

	public const int ToggleOarsmen = 111;

	public const int ChangeShipCamera = 112;

	public const int SelectShip = 113;

	public const int AttemptBoarding = 114;

	public const int ToggleRangedWeaponOrderMode = 115;

	public NavalShipControlsHotKeyCategory()
		: base("NavalShipControlsHotKeyCategory", 116)
	{
		GameAxisKey gameKey = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("MovementAxisY"));
		GameAxisKey gameKey2 = GenericGameKeyContext.Current.RegisteredGameAxisKeys.First((GameAxisKey g) => g.Id.Equals("MovementAxisX"));
		RegisterGameAxisKey(gameKey);
		RegisterGameAxisKey(gameKey2);
		RegisterGameKey(new GameKey(110, "ToggleSail", "NavalShipControlsHotKeyCategory", InputKey.Z, InputKey.ControllerLUp, GameKeyMainCategories.ShipControlsCategory));
		RegisterGameKey(new GameKey(111, "ToggleOarsmen", "NavalShipControlsHotKeyCategory", InputKey.X, InputKey.ControllerLDown, GameKeyMainCategories.ShipControlsCategory));
		RegisterGameKey(new GameKey(112, "ChangeShipCamera", "NavalShipControlsHotKeyCategory", InputKey.C, InputKey.ControllerLRight, GameKeyMainCategories.ShipControlsCategory));
		RegisterGameKey(new GameKey(113, "SelectShip", "NavalShipControlsHotKeyCategory", InputKey.E, InputKey.ControllerLThumb, GameKeyMainCategories.ShipControlsCategory));
		RegisterGameKey(new GameKey(114, "AttemptBoarding", "NavalShipControlsHotKeyCategory", InputKey.R, InputKey.ControllerRThumb, GameKeyMainCategories.ShipControlsCategory));
		RegisterGameKey(new GameKey(115, "ToggleRangedWeaponOrderMode", "NavalShipControlsHotKeyCategory", InputKey.RightMouseButton, InputKey.ControllerLTrigger, GameKeyMainCategories.ShipControlsCategory));
	}
}
