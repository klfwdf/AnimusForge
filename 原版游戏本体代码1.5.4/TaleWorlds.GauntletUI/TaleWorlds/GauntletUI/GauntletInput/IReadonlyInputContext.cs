using System.Numerics;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.GauntletInput;

public interface IReadonlyInputContext
{
	bool GetIsMouseActive();

	Vector2 GetMousePosition();

	Vector2 GetMouseMovement();

	InputKey[] GetClickKeys();

	InputKey[] GetAlternateClickKeys();

	Vector2 GetControllerLeftStickState();

	Vector2 GetControllerRightStickState();

	float GetMouseScrollDelta();
}
