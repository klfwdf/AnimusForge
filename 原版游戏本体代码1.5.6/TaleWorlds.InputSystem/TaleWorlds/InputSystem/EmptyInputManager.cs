using TaleWorlds.Library;

namespace TaleWorlds.InputSystem;

internal class EmptyInputManager : IInputManager
{
	public void ClearKeys()
	{
	}

	public InputKey[] GetClickKeys()
	{
		return new InputKey[0];
	}

	public string GetClipboardText()
	{
		return string.Empty;
	}

	public Input.ControllerTypes GetControllerType()
	{
		return Input.ControllerTypes.None;
	}

	public Vec2 GetDesktopResolution()
	{
		return Vec2.Zero;
	}

	public float GetGyroX()
	{
		return 0f;
	}

	public float GetGyroY()
	{
		return 0f;
	}

	public float GetGyroZ()
	{
		return 0f;
	}

	public Vec2 GetKeyState(InputKey key)
	{
		return Vec2.Zero;
	}

	public float GetMouseDeltaZ()
	{
		return 0f;
	}

	public float GetMouseMoveX()
	{
		return 0f;
	}

	public float GetMouseMoveY()
	{
		return 0f;
	}

	public float GetMousePositionX()
	{
		return 0f;
	}

	public float GetMousePositionY()
	{
		return 0f;
	}

	public float GetMouseScrollValue()
	{
		return 0f;
	}

	public float GetMouseSensitivity()
	{
		return 0f;
	}

	public float GetNormalizedMouseMoveX()
	{
		return 0f;
	}

	public float GetNormalizedMouseMoveY()
	{
		return 0f;
	}

	public Vec2 GetResolution()
	{
		return Vec2.Zero;
	}

	public int GetVirtualKeyCode(InputKey key)
	{
		return -1;
	}

	public bool IsAnyTouchActive()
	{
		return false;
	}

	public bool IsControllerConnected()
	{
		return false;
	}

	public bool IsKeyDown(InputKey key)
	{
		return false;
	}

	public bool IsKeyDownImmediate(InputKey key)
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

	public bool IsMouseActive()
	{
		return false;
	}

	public void PressKey(InputKey key)
	{
	}

	public void SetClipboardText(string text)
	{
	}

	public void SetCursorFriction(float frictionValue)
	{
	}

	public void SetCursorPosition(int x, int y)
	{
	}

	public void SetLightbarColor(float red, float green, float blue)
	{
	}

	public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
	{
	}

	public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
	{
	}

	public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
	{
	}

	public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
	{
	}

	public void UpdateKeyData(byte[] keyData)
	{
	}
}
