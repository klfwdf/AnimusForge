using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.Engine;

[ApplicationInterfaceBase]
internal interface IInput
{
	[EngineMethod("clear_keys", false, null, false)]
	void ClearKeys();

	[EngineMethod("get_mouse_sensitivity", false, null, false)]
	float GetMouseSensitivity();

	[EngineMethod("get_mouse_delta_z", false, null, false)]
	float GetMouseDeltaZ();

	[EngineMethod("is_mouse_active", false, null, false)]
	bool IsMouseActive();

	[EngineMethod("is_controller_connected", false, null, false)]
	bool IsControllerConnected();

	[EngineMethod("set_rumble_effect", false, null, false)]
	void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements);

	[EngineMethod("set_trigger_feedback", false, null, false)]
	void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength);

	[EngineMethod("set_trigger_weapon_effect", false, null, false)]
	void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength);

	[EngineMethod("set_trigger_vibration", false, null, false)]
	void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements);

	[EngineMethod("set_lightbar_color", false, null, false)]
	void SetLightbarColor(float red, float green, float blue);

	[EngineMethod("press_key", false, null, false)]
	void PressKey(InputKey key);

	[EngineMethod("get_virtual_key_code", false, null, false)]
	int GetVirtualKeyCode(InputKey key);

	[EngineMethod("get_controller_type", false, null, false)]
	int GetControllerType();

	[EngineMethod("set_clipboard_text", false, null, false)]
	void SetClipboardText(string text);

	[EngineMethod("get_clipboard_text", false, null, false)]
	string GetClipboardText();

	[EngineMethod("update_key_data", false, null, false)]
	void UpdateKeyData(byte[] keyData);

	[EngineMethod("get_mouse_move_x", false, null, false)]
	float GetMouseMoveX();

	[EngineMethod("get_mouse_move_y", false, null, false)]
	float GetMouseMoveY();

	[EngineMethod("get_gyro_x", false, null, false)]
	float GetGyroX();

	[EngineMethod("get_gyro_y", false, null, false)]
	float GetGyroY();

	[EngineMethod("get_gyro_z", false, null, false)]
	float GetGyroZ();

	[EngineMethod("get_mouse_position_x", false, null, false)]
	float GetMousePositionX();

	[EngineMethod("get_mouse_position_y", false, null, false)]
	float GetMousePositionY();

	[EngineMethod("get_mouse_scroll_value", false, null, false)]
	float GetMouseScrollValue();

	[EngineMethod("get_key_state", false, null, false)]
	Vec2 GetKeyState(InputKey key);

	[EngineMethod("is_key_down", false, null, true)]
	bool IsKeyDown(InputKey key);

	[EngineMethod("is_key_down_immediate", false, null, false)]
	bool IsKeyDownImmediate(InputKey key);

	[EngineMethod("is_key_pressed", false, null, true)]
	bool IsKeyPressed(InputKey key);

	[EngineMethod("is_key_released", false, null, false)]
	bool IsKeyReleased(InputKey key);

	[EngineMethod("is_any_touch_active", false, null, false)]
	bool IsAnyTouchActive();

	[EngineMethod("set_cursor_position", false, null, false)]
	void SetCursorPosition(int x, int y);

	[EngineMethod("set_cursor_friction_value", false, null, false)]
	void SetCursorFrictionValue(float frictionValue);
}
