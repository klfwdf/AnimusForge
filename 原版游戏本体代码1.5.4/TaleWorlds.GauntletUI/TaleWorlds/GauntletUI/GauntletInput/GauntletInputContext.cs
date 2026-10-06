using System.Numerics;
using TaleWorlds.InputSystem;

namespace TaleWorlds.GauntletUI.GauntletInput;

public class GauntletInputContext : IReadonlyInputContext
{
	private readonly IInputContext _inputContext;

	private bool _isMousePositionOverridden;

	private Vector2 _overrideMousePosition;

	public GauntletInputContext(IInputContext inputContext)
	{
		_inputContext = inputContext;
	}

	public bool GetIsMouseActive()
	{
		return _inputContext.GetIsMouseActive();
	}

	public Vector2 GetMousePosition()
	{
		if (_isMousePositionOverridden)
		{
			return _overrideMousePosition;
		}
		return _inputContext.GetPointerPosition();
	}

	public Vector2 GetMouseMovement()
	{
		return new Vector2(_inputContext.GetMouseMoveX(), _inputContext.GetMouseMoveY());
	}

	public InputKey[] GetClickKeys()
	{
		return Input.GetClickKeys();
	}

	public InputKey[] GetAlternateClickKeys()
	{
		return new InputKey[1] { InputKey.RightMouseButton };
	}

	public float GetMouseScrollDelta()
	{
		return _inputContext.GetDeltaMouseScroll();
	}

	public Vector2 GetControllerLeftStickState()
	{
		return (Vector2)_inputContext.GetControllerLeftStickState();
	}

	public Vector2 GetControllerRightStickState()
	{
		return (Vector2)_inputContext.GetControllerRightStickState();
	}

	public void SetMousePositionOverride(Vector2 mousePosition)
	{
		_isMousePositionOverridden = true;
		_overrideMousePosition = mousePosition;
	}

	public void ResetMousePositionOverride()
	{
		_isMousePositionOverridden = false;
	}
}
