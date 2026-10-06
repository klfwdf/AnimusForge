using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.InputSystem;

public class EngineInputManager : IInputManager
{
	float IInputManager.GetMousePositionX()
	{
		return EngineApplicationInterface.IInput.GetMousePositionX();
	}

	float IInputManager.GetMousePositionY()
	{
		return EngineApplicationInterface.IInput.GetMousePositionY();
	}

	float IInputManager.GetMouseScrollValue()
	{
		return EngineApplicationInterface.IInput.GetMouseScrollValue();
	}

	bool IInputManager.IsMouseActive()
	{
		return EngineApplicationInterface.IInput.IsMouseActive();
	}

	bool IInputManager.IsControllerConnected()
	{
		return EngineApplicationInterface.IInput.IsControllerConnected();
	}

	void IInputManager.PressKey(InputKey key)
	{
		EngineApplicationInterface.IInput.PressKey(key);
	}

	void IInputManager.ClearKeys()
	{
		EngineApplicationInterface.IInput.ClearKeys();
	}

	int IInputManager.GetVirtualKeyCode(InputKey key)
	{
		return EngineApplicationInterface.IInput.GetVirtualKeyCode(key);
	}

	void IInputManager.SetClipboardText(string text)
	{
		EngineApplicationInterface.IInput.SetClipboardText(text);
	}

	string IInputManager.GetClipboardText()
	{
		return EngineApplicationInterface.IInput.GetClipboardText();
	}

	float IInputManager.GetMouseMoveX()
	{
		return EngineApplicationInterface.IInput.GetMouseMoveX();
	}

	float IInputManager.GetMouseMoveY()
	{
		return EngineApplicationInterface.IInput.GetMouseMoveY();
	}

	float IInputManager.GetNormalizedMouseMoveX()
	{
		return EngineApplicationInterface.IInput.GetMouseMoveX() / Screen.RealScreenResolutionWidth;
	}

	float IInputManager.GetNormalizedMouseMoveY()
	{
		return EngineApplicationInterface.IInput.GetMouseMoveY() / Screen.RealScreenResolutionHeight;
	}

	float IInputManager.GetGyroX()
	{
		return EngineApplicationInterface.IInput.GetGyroX();
	}

	float IInputManager.GetGyroY()
	{
		return EngineApplicationInterface.IInput.GetGyroY();
	}

	float IInputManager.GetGyroZ()
	{
		return EngineApplicationInterface.IInput.GetGyroZ();
	}

	float IInputManager.GetMouseSensitivity()
	{
		return EngineApplicationInterface.IInput.GetMouseSensitivity();
	}

	float IInputManager.GetMouseDeltaZ()
	{
		return EngineApplicationInterface.IInput.GetMouseDeltaZ();
	}

	void IInputManager.UpdateKeyData(byte[] keyData)
	{
		EngineApplicationInterface.IInput.UpdateKeyData(keyData);
	}

	Vec2 IInputManager.GetKeyState(InputKey key)
	{
		return EngineApplicationInterface.IInput.GetKeyState(key);
	}

	bool IInputManager.IsKeyPressed(InputKey key)
	{
		return EngineApplicationInterface.IInput.IsKeyPressed(key);
	}

	bool IInputManager.IsKeyDown(InputKey key)
	{
		return EngineApplicationInterface.IInput.IsKeyDown(key);
	}

	bool IInputManager.IsKeyDownImmediate(InputKey key)
	{
		return EngineApplicationInterface.IInput.IsKeyDownImmediate(key);
	}

	bool IInputManager.IsKeyReleased(InputKey key)
	{
		return EngineApplicationInterface.IInput.IsKeyReleased(key);
	}

	Vec2 IInputManager.GetResolution()
	{
		return Screen.RealScreenResolution;
	}

	Vec2 IInputManager.GetDesktopResolution()
	{
		return Screen.DesktopResolution;
	}

	void IInputManager.SetCursorPosition(int x, int y)
	{
		float num = 1f;
		float num2 = 1f;
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DisplayMode) != 0f)
		{
			num = Input.DesktopResolution.X / Input.Resolution.X;
			num2 = Input.DesktopResolution.Y / Input.Resolution.Y;
		}
		EngineApplicationInterface.IInput.SetCursorPosition((int)((float)x * num), (int)((float)y * num2));
	}

	void IInputManager.SetCursorFriction(float frictionValue)
	{
		EngineApplicationInterface.IInput.SetCursorFrictionValue(frictionValue);
	}

	InputKey[] IInputManager.GetClickKeys()
	{
		InputKey inputKey = (EngineApplicationInterface.IScreen.IsEnterButtonCross() ? InputKey.ControllerRDown : InputKey.ControllerRRight);
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) == 0f)
		{
			return new InputKey[2]
			{
				InputKey.LeftMouseButton,
				inputKey
			};
		}
		return new InputKey[3]
		{
			InputKey.LeftMouseButton,
			inputKey,
			InputKey.ControllerLOptionTap
		};
	}

	public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
	{
		EngineApplicationInterface.IInput.SetRumbleEffect(lowFrequencyLevels, lowFrequencyDurations, numLowFrequencyElements, highFrequencyLevels, highFrequencyDurations, numHighFrequencyElements);
	}

	public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
	{
		EngineApplicationInterface.IInput.SetTriggerFeedback(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
	}

	public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
	{
		EngineApplicationInterface.IInput.SetTriggerWeaponEffect(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
	}

	public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
	{
		EngineApplicationInterface.IInput.SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, leftTriggerDurations, numLeftTriggerElements, rightTriggerAmplitudes, rightTriggerFrequencies, rightTriggerDurations, numRightTriggerElements);
	}

	public void SetLightbarColor(float red, float green, float blue)
	{
		EngineApplicationInterface.IInput.SetLightbarColor(red, green, blue);
	}

	Input.ControllerTypes IInputManager.GetControllerType()
	{
		return (Input.ControllerTypes)EngineApplicationInterface.IInput.GetControllerType();
	}

	bool IInputManager.IsAnyTouchActive()
	{
		return EngineApplicationInterface.IInput.IsAnyTouchActive();
	}
}
