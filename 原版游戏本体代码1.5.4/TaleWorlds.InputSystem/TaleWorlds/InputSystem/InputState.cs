using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public class InputState
{
	private Vec2 _mousePositionRanged;

	private Vec2 _mousePositionRangedDevice;

	private Vec2 _mousePositionPixel;

	private Vec2 _mousePositionPixelDevice;

	public Vec2 NativeResolution => Input.Resolution;

	public Vec2 MousePositionRanged
	{
		get
		{
			return _mousePositionRanged;
		}
		set
		{
			_mousePositionRanged = value;
			_mousePositionPixel = new Vec2(_mousePositionRanged.x * NativeResolution.x, _mousePositionRanged.y * NativeResolution.y);
		}
	}

	public Vec2 OldMousePositionRanged { get; private set; }

	public bool MousePositionChanged { get; private set; }

	public Vec2 MousePositionPixel
	{
		get
		{
			return _mousePositionPixel;
		}
		set
		{
			_mousePositionPixel = value;
			_mousePositionRanged = new Vec2(_mousePositionPixel.x / Input.Resolution.x, _mousePositionPixel.y / NativeResolution.y);
		}
	}

	public Vec2 OldMousePositionPixel { get; private set; }

	public float MouseScrollValue { get; private set; }

	public bool MouseScrollChanged { get; private set; }

	public InputState()
	{
		MousePositionRanged = default(Vec2);
		OldMousePositionRanged = default(Vec2);
		MousePositionPixel = default(Vec2);
		OldMousePositionPixel = default(Vec2);
		_mousePositionRanged = new Vec2(0f, 0f);
		_mousePositionPixel = new Vec2(0f, 0f);
		_mousePositionPixelDevice = new Vec2(0f, 0f);
		_mousePositionRangedDevice = new Vec2(0f, 0f);
	}

	public bool UpdateMousePosition(float mousePositionX, float mousePositionY)
	{
		OldMousePositionRanged = new Vec2(_mousePositionRangedDevice.x, _mousePositionRangedDevice.y);
		_mousePositionRangedDevice = new Vec2(mousePositionX, mousePositionY);
		OldMousePositionPixel = new Vec2(_mousePositionPixelDevice.x, _mousePositionPixelDevice.y);
		_mousePositionPixelDevice = new Vec2(_mousePositionRangedDevice.x * NativeResolution.x, _mousePositionRangedDevice.y * NativeResolution.y);
		if (_mousePositionRangedDevice.x == OldMousePositionRanged.x && _mousePositionRangedDevice.y == OldMousePositionRanged.y)
		{
			MousePositionChanged = false;
		}
		else
		{
			MousePositionChanged = true;
			MousePositionPixel = _mousePositionPixelDevice;
			MousePositionRanged = _mousePositionRangedDevice;
		}
		return MousePositionChanged;
	}

	public bool UpdateMouseScroll(float mouseScrollValue)
	{
		if (!MouseScrollValue.Equals(mouseScrollValue))
		{
			MouseScrollValue = mouseScrollValue;
			MouseScrollChanged = true;
		}
		else
		{
			MouseScrollChanged = false;
		}
		return MouseScrollChanged;
	}
}
