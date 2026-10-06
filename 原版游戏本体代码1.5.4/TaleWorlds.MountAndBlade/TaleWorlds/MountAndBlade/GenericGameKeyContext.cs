using TaleWorlds.InputSystem;

namespace TaleWorlds.MountAndBlade;

public sealed class GenericGameKeyContext : GameKeyContext
{
	public const string CategoryId = "Generic";

	public const int Up = 0;

	public const int Down = 1;

	public const int Right = 3;

	public const int Left = 2;

	public const string MovementAxisX = "MovementAxisX";

	public const string MovementAxisY = "MovementAxisY";

	public const string CameraAxisX = "CameraAxisX";

	public const string CameraAxisY = "CameraAxisY";

	public const int Leave = 4;

	public const int ShowIndicators = 5;

	public static GenericGameKeyContext Current { get; private set; }

	public GenericGameKeyContext()
		: base("Generic", 116)
	{
		Current = this;
		RegisterHotKeys();
		RegisterGameKeys();
		RegisterGameAxisKeys();
	}

	private void RegisterHotKeys()
	{
	}

	private void RegisterGameKeys()
	{
		GameKey gameKey = new GameKey(0, "Up", "Generic", InputKey.W, InputKey.ControllerLStickUp, GameKeyMainCategories.ActionCategory);
		GameKey gameKey2 = new GameKey(1, "Down", "Generic", InputKey.S, InputKey.ControllerLStickDown, GameKeyMainCategories.ActionCategory);
		GameKey gameKey3 = new GameKey(2, "Left", "Generic", InputKey.A, InputKey.ControllerLStickLeft, GameKeyMainCategories.ActionCategory);
		GameKey gameKey4 = new GameKey(3, "Right", "Generic", InputKey.D, InputKey.ControllerLStickRight, GameKeyMainCategories.ActionCategory);
		RegisterGameKey(gameKey);
		RegisterGameKey(gameKey2);
		RegisterGameKey(gameKey3);
		RegisterGameKey(gameKey4);
		RegisterGameKey(new GameKey(4, "Leave", "Generic", InputKey.Tab, InputKey.ControllerRRight, GameKeyMainCategories.ActionCategory));
		RegisterGameKey(new GameKey(5, "ShowIndicators", "Generic", InputKey.LeftAlt, InputKey.ControllerLBumper, GameKeyMainCategories.ActionCategory));
		RegisterGameAxisKey(new GameAxisKey("MovementAxisX", InputKey.ControllerLStick, gameKey4, gameKey3));
		RegisterGameAxisKey(new GameAxisKey("MovementAxisY", InputKey.ControllerLStick, gameKey, gameKey2, GameAxisKey.AxisType.Y));
		RegisterGameAxisKey(new GameAxisKey("CameraAxisX", InputKey.ControllerRStick, null, null));
		RegisterGameAxisKey(new GameAxisKey("CameraAxisY", InputKey.ControllerRStick, null, null, GameAxisKey.AxisType.Y));
	}

	private void RegisterGameAxisKeys()
	{
	}
}
