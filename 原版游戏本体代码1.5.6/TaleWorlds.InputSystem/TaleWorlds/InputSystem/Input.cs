using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

public static class Input
{
	public enum ControllerTypes
	{
		None = 0,
		Xbox = 1,
		PlayStationDualShock = 2,
		PlayStationDualSense = 4
	}

	public const int NumberOfKeys = 256;

	private static byte[] keyData;

	private static IInputManager _emptyInputManager;

	private static IInputManager _inputManager;

	public static Action OnGamepadActiveStateChanged;

	private static bool _isGamepadActive;

	private static bool _isAnyTouchActive;

	public static Action<ControllerTypes> OnControllerTypeChanged;

	private static ControllerTypes _controllerType;

	public static InputState InputState { get; private set; }

	public static IInputContext DebugInput { get; private set; }

	public static IInputManager InputManager
	{
		get
		{
			if (IsOnScreenKeyboardActive)
			{
				return _emptyInputManager;
			}
			return _inputManager;
		}
	}

	public static Vec2 Resolution => _inputManager.GetResolution();

	public static Vec2 DesktopResolution => _inputManager.GetDesktopResolution();

	public static bool IsOnScreenKeyboardActive { get; set; }

	public static bool IsMouseActive => InputManager.IsMouseActive();

	public static bool IsControllerConnected => InputManager.IsControllerConnected();

	public static bool IsGamepadActive
	{
		get
		{
			return _isGamepadActive;
		}
		private set
		{
			if (value != _isGamepadActive)
			{
				_isGamepadActive = value;
				OnGamepadActiveStateChanged?.Invoke();
			}
		}
	}

	public static bool IsAnyTouchActive
	{
		get
		{
			return _isAnyTouchActive;
		}
		private set
		{
			if (value != _isAnyTouchActive)
			{
				_isAnyTouchActive = value;
			}
		}
	}

	public static ControllerTypes ControllerType
	{
		get
		{
			return _controllerType;
		}
		private set
		{
			if (value != _controllerType)
			{
				_controllerType = value;
				OnControllerTypeChanged?.Invoke(value);
			}
		}
	}

	public static float MouseMoveX => InputManager.GetMouseMoveX();

	public static float MouseMoveY => InputManager.GetMouseMoveY();

	public static float GyroX => InputManager.GetGyroX();

	public static float GyroY => InputManager.GetGyroY();

	public static float GyroZ => InputManager.GetGyroZ();

	public static float MouseSensitivity => InputManager.GetMouseSensitivity();

	public static float DeltaMouseScroll => InputManager.GetMouseDeltaZ();

	public static Vec2 MousePositionRanged => InputState.MousePositionRanged;

	public static Vec2 MousePositionPixel => InputState.MousePositionPixel;

	public static bool IsMousePositionUpdated { get; private set; }

	public static bool IsMouseScrollChanged { get; private set; }

	public static bool IsPlaystation(this ControllerTypes controllerType)
	{
		return controllerType.HasAnyFlag((ControllerTypes)6);
	}

	public static void Initialize(IInputManager inputManager, IInputContext debugInput)
	{
		_emptyInputManager = new EmptyInputManager();
		_inputManager = inputManager;
		InputState = new InputState();
		keyData = new byte[256];
		DebugInput = new EmptyInputContext();
	}

	public static void UpdateKeyData(byte[] keyData)
	{
		InputManager.UpdateKeyData(keyData);
	}

	public static float GetMouseMoveX()
	{
		return InputManager.GetMouseMoveX();
	}

	public static float GetMouseMoveY()
	{
		return InputManager.GetMouseMoveY();
	}

	public static float GetNormalizedMouseMoveX()
	{
		return InputManager.GetNormalizedMouseMoveX();
	}

	public static float GetNormalizedMouseMoveY()
	{
		return InputManager.GetNormalizedMouseMoveY();
	}

	public static float GetGyroX()
	{
		return InputManager.GetGyroX();
	}

	public static float GetGyroY()
	{
		return InputManager.GetGyroY();
	}

	public static float GetGyroZ()
	{
		return InputManager.GetGyroZ();
	}

	public static Vec2 GetKeyState(InputKey key)
	{
		return InputManager.GetKeyState(key);
	}

	public static bool IsKeyPressed(InputKey key)
	{
		return InputManager.IsKeyPressed(key);
	}

	public static bool IsKeyDown(InputKey key)
	{
		return InputManager.IsKeyDown(key);
	}

	public static bool IsKeyDownImmediate(InputKey key)
	{
		return InputManager.IsKeyDownImmediate(key);
	}

	public static bool IsKeyReleased(InputKey key)
	{
		return InputManager.IsKeyReleased(key);
	}

	public static bool IsControlOrShiftNotDown()
	{
		if (!InputKey.LeftControl.IsDown() && !InputKey.RightControl.IsDown() && !InputKey.LeftShift.IsDown())
		{
			return !InputKey.RightShift.IsDown();
		}
		return false;
	}

	public static ControllerTypes GetPrimaryControllerType()
	{
		return ControllerTypes.Xbox;
	}

	public static int GetFirstKeyPressedInRange(int startKeyNo)
	{
		int result = -1;
		for (int i = startKeyNo; i < 256; i++)
		{
			if (IsKeyPressed((InputKey)i))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int GetFirstKeyDownInRange(int startKeyNo)
	{
		int result = -1;
		for (int i = startKeyNo; i < 256; i++)
		{
			if (IsKeyDown((InputKey)i))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static int GetFirstKeyReleasedInRange(int startKeyNo)
	{
		int result = -1;
		for (int i = startKeyNo; i < 256; i++)
		{
			if (IsKeyReleased((InputKey)i))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public static void PressKey(InputKey key)
	{
		InputManager.PressKey(key);
	}

	public static void ClearKeys()
	{
		InputManager.ClearKeys();
	}

	public static int GetVirtualKeyCode(InputKey key)
	{
		return InputManager.GetVirtualKeyCode(key);
	}

	public static bool IsDown(this InputKey key)
	{
		return IsKeyDown(key);
	}

	public static bool IsPressed(this InputKey key)
	{
		return IsKeyPressed(key);
	}

	public static bool IsReleased(this InputKey key)
	{
		return IsKeyReleased(key);
	}

	public static void SetClipboardText(string text)
	{
		InputManager.SetClipboardText(text);
	}

	public static string GetClipboardText()
	{
		return InputManager.GetClipboardText();
	}

	public static void Update()
	{
		if (!IsOnScreenKeyboardActive)
		{
			float mousePositionX = InputManager.GetMousePositionX();
			float mousePositionY = InputManager.GetMousePositionY();
			float mouseScrollValue = InputManager.GetMouseScrollValue();
			IsMousePositionUpdated = InputState.UpdateMousePosition(mousePositionX, mousePositionY);
			IsMouseScrollChanged = InputState.UpdateMouseScroll(mouseScrollValue);
			IsGamepadActive = IsControllerConnected && !IsMouseActive;
			IsAnyTouchActive = InputManager.IsAnyTouchActive();
			ControllerType = InputManager.GetControllerType();
			UpdateKeyData(keyData);
		}
	}

	public static bool IsControllerKey(InputKey key)
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
		case InputKey.LeftMouseButton:
		case InputKey.RightMouseButton:
		case InputKey.MiddleMouseButton:
		case InputKey.X1MouseButton:
		case InputKey.X2MouseButton:
		case InputKey.MouseScrollUp:
		case InputKey.MouseScrollDown:
			return false;
		default:
			return true;
		}
	}

	public static void SetMousePosition(int x, int y)
	{
		InputManager.SetCursorPosition(x, y);
	}

	public static void SetCursorFriction(float frictionValue)
	{
		InputManager.SetCursorFriction(frictionValue);
	}

	public static InputKey[] GetClickKeys()
	{
		return InputManager.GetClickKeys();
	}

	public static void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
	{
		InputManager.SetRumbleEffect(lowFrequencyLevels, lowFrequencyDurations, numLowFrequencyElements, highFrequencyLevels, highFrequencyDurations, numHighFrequencyElements);
	}

	public static void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
	{
		InputManager.SetTriggerFeedback(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
	}

	public static void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
	{
		InputManager.SetTriggerWeaponEffect(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
	}

	public static void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
	{
		InputManager.SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, leftTriggerDurations, numLeftTriggerElements, rightTriggerAmplitudes, rightTriggerFrequencies, rightTriggerDurations, numRightTriggerElements);
	}

	public static void SetLightbarColor(float red, float green, float blue)
	{
		InputManager.SetLightbarColor(red, green, blue);
	}
}
