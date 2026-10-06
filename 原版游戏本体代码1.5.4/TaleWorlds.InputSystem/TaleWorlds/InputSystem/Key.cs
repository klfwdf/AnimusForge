using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

[Serializable]
public class Key
{
	public enum InputType
	{
		Invalid = -1,
		Keyboard,
		MouseButton,
		MouseWheel,
		Controller
	}

	public bool IsKeyboardInput { get; private set; }

	public bool IsMouseButtonInput { get; private set; }

	public bool IsMouseWheelInput { get; private set; }

	public bool IsControllerInput { get; private set; }

	public InputKey InputKey { get; private set; }

	public Key(InputKey key)
	{
		ChangeKey(key);
	}

	public Key()
	{
	}

	public void ChangeKey(InputKey key)
	{
		InputKey = key;
		IsKeyboardInput = GetInputType(key) == InputType.Keyboard;
		IsMouseButtonInput = GetInputType(key) == InputType.MouseButton;
		IsMouseWheelInput = GetInputType(key) == InputType.MouseWheel;
		IsControllerInput = GetInputType(key) == InputType.Controller;
	}

	internal bool IsPressed()
	{
		return Input.IsKeyPressed(InputKey);
	}

	internal bool IsDown()
	{
		return Input.IsKeyDown(InputKey);
	}

	internal bool IsDownImmediate()
	{
		if (IsKeyboardInput || IsMouseButtonInput)
		{
			return Input.IsKeyDownImmediate(InputKey);
		}
		return Input.IsKeyDown(InputKey);
	}

	internal bool IsReleased()
	{
		return Input.IsKeyReleased(InputKey);
	}

	internal Vec2 GetKeyState()
	{
		return Input.GetKeyState(InputKey);
	}

	public override string ToString()
	{
		if (IsKeyboardInput)
		{
			int virtualKeyCode = Input.GetVirtualKeyCode(InputKey);
			if (virtualKeyCode != 0)
			{
				VirtualKeyCode virtualKeyCode2 = (VirtualKeyCode)virtualKeyCode;
				return virtualKeyCode2.ToString();
			}
		}
		return InputKey.ToString();
	}

	public override bool Equals(object obj)
	{
		return (obj as Key).InputKey == InputKey;
	}

	public override int GetHashCode()
	{
		return base.GetHashCode();
	}

	public static bool operator ==(Key k1, Key k2)
	{
		if ((object)k1 == k2)
		{
			return true;
		}
		if ((object)k1 == null || (object)k2 == null)
		{
			return false;
		}
		return k1.InputKey == k2.InputKey;
	}

	public static bool operator !=(Key k1, Key k2)
	{
		return !(k1 == k2);
	}

	public static bool IsLeftAnalogInput(InputKey key)
	{
		if (key == InputKey.ControllerLStick || (uint)(key - 232) <= 3u || key == InputKey.ControllerLThumb)
		{
			return true;
		}
		return false;
	}

	public static bool IsLeftBumperOrTriggerInput(InputKey key)
	{
		if (key == InputKey.ControllerLBumper || key == InputKey.ControllerLTrigger)
		{
			return true;
		}
		return false;
	}

	public static bool IsRightBumperOrTriggerInput(InputKey key)
	{
		if (key == InputKey.ControllerRBumper || key == InputKey.ControllerRTrigger)
		{
			return true;
		}
		return false;
	}

	public static bool IsFaceKeyInput(InputKey key)
	{
		if ((uint)(key - 244) <= 3u)
		{
			return true;
		}
		return false;
	}

	public static bool IsRightAnalogInput(InputKey key)
	{
		if (key == InputKey.ControllerRStick || (uint)(key - 236) <= 3u || key == InputKey.ControllerRThumb)
		{
			return true;
		}
		return false;
	}

	public static bool IsDpadInput(InputKey key)
	{
		if ((uint)(key - 240) <= 3u)
		{
			return true;
		}
		return false;
	}

	public static InputType GetInputType(InputKey key)
	{
		switch (key)
		{
		case InputKey.Escape:
		case InputKey.D1:
		case InputKey.D2:
		case InputKey.D3:
		case InputKey.D4:
		case InputKey.D5:
		case InputKey.D6:
		case InputKey.D7:
		case InputKey.D8:
		case InputKey.D9:
		case InputKey.D0:
		case InputKey.Minus:
		case InputKey.Equals:
		case InputKey.BackSpace:
		case InputKey.Tab:
		case InputKey.Q:
		case InputKey.W:
		case InputKey.E:
		case InputKey.R:
		case InputKey.T:
		case InputKey.Y:
		case InputKey.U:
		case InputKey.I:
		case InputKey.O:
		case InputKey.P:
		case InputKey.OpenBraces:
		case InputKey.CloseBraces:
		case InputKey.Enter:
		case InputKey.LeftControl:
		case InputKey.A:
		case InputKey.S:
		case InputKey.D:
		case InputKey.F:
		case InputKey.G:
		case InputKey.H:
		case InputKey.J:
		case InputKey.K:
		case InputKey.L:
		case InputKey.SemiColon:
		case InputKey.Apostrophe:
		case InputKey.Tilde:
		case InputKey.LeftShift:
		case InputKey.BackSlash:
		case InputKey.Z:
		case InputKey.X:
		case InputKey.C:
		case InputKey.V:
		case InputKey.B:
		case InputKey.N:
		case InputKey.M:
		case InputKey.Comma:
		case InputKey.Period:
		case InputKey.Slash:
		case InputKey.RightShift:
		case InputKey.NumpadMultiply:
		case InputKey.LeftAlt:
		case InputKey.Space:
		case InputKey.CapsLock:
		case InputKey.F1:
		case InputKey.F2:
		case InputKey.F3:
		case InputKey.F4:
		case InputKey.F5:
		case InputKey.F6:
		case InputKey.F7:
		case InputKey.F8:
		case InputKey.F9:
		case InputKey.F10:
		case InputKey.Numpad7:
		case InputKey.Numpad8:
		case InputKey.Numpad9:
		case InputKey.NumpadMinus:
		case InputKey.Numpad4:
		case InputKey.Numpad5:
		case InputKey.Numpad6:
		case InputKey.NumpadPlus:
		case InputKey.Numpad1:
		case InputKey.Numpad2:
		case InputKey.Numpad3:
		case InputKey.Numpad0:
		case InputKey.NumpadPeriod:
		case InputKey.Extended:
		case InputKey.F11:
		case InputKey.F12:
		case InputKey.NumpadEnter:
		case InputKey.RightControl:
		case InputKey.NumpadSlash:
		case InputKey.RightAlt:
		case InputKey.NumLock:
		case InputKey.Home:
		case InputKey.Up:
		case InputKey.PageUp:
		case InputKey.Left:
		case InputKey.Right:
		case InputKey.End:
		case InputKey.Down:
		case InputKey.PageDown:
		case InputKey.Insert:
		case InputKey.Delete:
			return InputType.Keyboard;
		case InputKey.LeftMouseButton:
		case InputKey.RightMouseButton:
		case InputKey.MiddleMouseButton:
		case InputKey.X1MouseButton:
		case InputKey.X2MouseButton:
			return InputType.MouseButton;
		case InputKey.MouseScrollUp:
		case InputKey.MouseScrollDown:
			return InputType.MouseWheel;
		case InputKey.ControllerLStick:
		case InputKey.ControllerRStick:
		case InputKey.ControllerLOptionTap:
		case InputKey.ControllerLStickUp:
		case InputKey.ControllerLStickDown:
		case InputKey.ControllerLStickLeft:
		case InputKey.ControllerLStickRight:
		case InputKey.ControllerRStickUp:
		case InputKey.ControllerRStickDown:
		case InputKey.ControllerRStickLeft:
		case InputKey.ControllerRStickRight:
		case InputKey.ControllerLUp:
		case InputKey.ControllerLDown:
		case InputKey.ControllerLLeft:
		case InputKey.ControllerLRight:
		case InputKey.ControllerRUp:
		case InputKey.ControllerRDown:
		case InputKey.ControllerRLeft:
		case InputKey.ControllerRRight:
		case InputKey.ControllerLBumper:
		case InputKey.ControllerRBumper:
		case InputKey.ControllerLOption:
		case InputKey.ControllerROption:
		case InputKey.ControllerLThumb:
		case InputKey.ControllerRThumb:
		case InputKey.ControllerLTrigger:
		case InputKey.ControllerRTrigger:
			return InputType.Controller;
		default:
			return InputType.Invalid;
		}
	}
}
