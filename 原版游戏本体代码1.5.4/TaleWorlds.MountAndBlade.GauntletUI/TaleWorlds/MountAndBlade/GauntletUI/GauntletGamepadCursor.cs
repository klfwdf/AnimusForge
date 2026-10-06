using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletGamepadCursor : GlobalLayer
{
	private GamepadCursorViewModel _dataSource;

	private GauntletLayer _layer;

	private static GauntletGamepadCursor _current;

	public GauntletGamepadCursor()
	{
		_dataSource = new GamepadCursorViewModel();
		_layer = new GauntletLayer("GamepadCusor", 115001);
		_layer.LoadMovie("GamepadCursor", _dataSource);
		_layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false, InputUsageMask.Invalid);
		base.Layer = _layer;
	}

	public static void Initialize()
	{
		if (_current == null)
		{
			_current = new GauntletGamepadCursor();
			ScreenManager.AddGlobalLayer(_current, isFocusable: false);
		}
	}

	protected override void OnLateTick(float dt)
	{
		base.OnLateTick(dt);
		if (ScreenManager.IsMouseCursorHidden())
		{
			_dataSource.IsGamepadCursorVisible = true;
			_dataSource.IsConsoleMouseVisible = false;
			Vec2 cursorPosition = GetCursorPosition();
			_dataSource.CursorPositionX = cursorPosition.X;
			_dataSource.CursorPositionY = cursorPosition.Y;
		}
		else
		{
			_dataSource.IsGamepadCursorVisible = false;
			_dataSource.IsConsoleMouseVisible = false;
		}
	}

	private Vec2 GetCursorPosition()
	{
		Vec2 mousePositionPixel = Input.MousePositionPixel;
		Vec2 vec = Vec2.One - ScreenManager.UsableArea;
		float num = vec.x * Screen.RealScreenResolution.x / 2f;
		float num2 = vec.y * Screen.RealScreenResolution.y / 2f;
		return new Vec2(mousePositionPixel.X - num, mousePositionPixel.Y - num2);
	}
}
