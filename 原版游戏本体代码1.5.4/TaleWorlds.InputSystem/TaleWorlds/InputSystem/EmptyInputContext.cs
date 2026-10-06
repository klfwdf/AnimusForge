using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public sealed class EmptyInputContext : IInputContext
{
	public int GetPointerX()
	{
		return 0;
	}

	public int GetPointerY()
	{
		return 0;
	}

	public Vector2 GetPointerPosition()
	{
		return new Vector2(0f, 0f);
	}

	public bool IsGameKeyDown(int gameKey)
	{
		return false;
	}

	public bool IsGameKeyDownImmediate(int gameKey)
	{
		return false;
	}

	public bool IsGameKeyPressed(int gameKey)
	{
		return false;
	}

	public bool IsGameKeyReleased(int gameKey)
	{
		return false;
	}

	public float GetGameKeyAxis(string gameAxisKey)
	{
		return 0f;
	}

	public bool IsHotKeyDown(string hotKey)
	{
		return false;
	}

	public bool IsHotKeyReleased(string hotKey)
	{
		return false;
	}

	public bool IsHotKeyPressed(string hotKey)
	{
		return false;
	}

	public bool IsHotKeyDoublePressed(string hotKey)
	{
		return false;
	}

	public Vec2 GetKeyState(InputKey key)
	{
		return new Vec2(0f, 0f);
	}

	public bool IsKeyDown(InputKey key)
	{
		return false;
	}

	public bool IsKeyPressed(InputKey key)
	{
		return false;
	}

	public bool IsKeyReleased(InputKey key)
	{
		return false;
	}

	public float GetMouseMoveX()
	{
		return 0f;
	}

	public float GetMouseMoveY()
	{
		return 0f;
	}

	public bool GetIsMouseActive()
	{
		return false;
	}

	public Vec2 GetMousePositionPixel()
	{
		return default(Vec2);
	}

	public float GetDeltaMouseScroll()
	{
		return 0f;
	}

	public bool GetIsControllerConnected()
	{
		return false;
	}

	public Vec2 GetMousePositionRanged()
	{
		return default(Vec2);
	}

	public float GetMouseSensitivity()
	{
		return 0f;
	}

	public bool IsControlDown()
	{
		return false;
	}

	public bool IsShiftDown()
	{
		return false;
	}

	public bool IsAltDown()
	{
		return false;
	}

	public Vec2 GetControllerRightStickState()
	{
		return default(Vec2);
	}

	public Vec2 GetControllerLeftStickState()
	{
		return default(Vec2);
	}

	public InputKey[] GetClickKeys()
	{
		return new InputKey[2]
		{
			InputKey.LeftMouseButton,
			InputKey.ControllerRDown
		};
	}
}
