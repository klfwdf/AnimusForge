using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public interface IInputContext
{
	int GetPointerX();

	int GetPointerY();

	Vector2 GetPointerPosition();

	bool IsGameKeyDown(int gameKey);

	bool IsGameKeyDownImmediate(int gameKey);

	bool IsGameKeyReleased(int gameKey);

	bool IsGameKeyPressed(int gameKey);

	float GetGameKeyAxis(string gameKey);

	bool IsHotKeyDown(string gameKey);

	bool IsHotKeyReleased(string gameKey);

	bool IsHotKeyPressed(string gameKey);

	bool IsHotKeyDoublePressed(string gameKey);

	bool IsKeyDown(InputKey key);

	bool IsKeyPressed(InputKey key);

	bool IsKeyReleased(InputKey key);

	Vec2 GetKeyState(InputKey key);

	float GetMouseMoveX();

	float GetMouseMoveY();

	Vec2 GetControllerRightStickState();

	Vec2 GetControllerLeftStickState();

	float GetDeltaMouseScroll();

	bool GetIsControllerConnected();

	bool GetIsMouseActive();

	Vec2 GetMousePositionRanged();

	Vec2 GetMousePositionPixel();

	float GetMouseSensitivity();

	bool IsControlDown();

	bool IsShiftDown();

	bool IsAltDown();

	InputKey[] GetClickKeys();
}
