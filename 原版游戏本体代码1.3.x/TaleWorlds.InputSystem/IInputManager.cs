using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x0200000A RID: 10
	public interface IInputManager
	{
		// Token: 0x060000CC RID: 204
		float GetMousePositionX();

		// Token: 0x060000CD RID: 205
		float GetMousePositionY();

		// Token: 0x060000CE RID: 206
		float GetMouseScrollValue();

		// Token: 0x060000CF RID: 207
		Input.ControllerTypes GetControllerType();

		// Token: 0x060000D0 RID: 208
		bool IsMouseActive();

		// Token: 0x060000D1 RID: 209
		bool IsControllerConnected();

		// Token: 0x060000D2 RID: 210
		bool IsAnyTouchActive();

		// Token: 0x060000D3 RID: 211
		void PressKey(InputKey key);

		// Token: 0x060000D4 RID: 212
		void ClearKeys();

		// Token: 0x060000D5 RID: 213
		int GetVirtualKeyCode(InputKey key);

		// Token: 0x060000D6 RID: 214
		void SetClipboardText(string text);

		// Token: 0x060000D7 RID: 215
		string GetClipboardText();

		// Token: 0x060000D8 RID: 216
		float GetMouseMoveX();

		// Token: 0x060000D9 RID: 217
		float GetMouseMoveY();

		// Token: 0x060000DA RID: 218
		float GetNormalizedMouseMoveX();

		// Token: 0x060000DB RID: 219
		float GetNormalizedMouseMoveY();

		// Token: 0x060000DC RID: 220
		float GetGyroX();

		// Token: 0x060000DD RID: 221
		float GetGyroY();

		// Token: 0x060000DE RID: 222
		float GetGyroZ();

		// Token: 0x060000DF RID: 223
		float GetMouseSensitivity();

		// Token: 0x060000E0 RID: 224
		float GetMouseDeltaZ();

		// Token: 0x060000E1 RID: 225
		void UpdateKeyData(byte[] keyData);

		// Token: 0x060000E2 RID: 226
		Vec2 GetKeyState(InputKey key);

		// Token: 0x060000E3 RID: 227
		bool IsKeyPressed(InputKey key);

		// Token: 0x060000E4 RID: 228
		bool IsKeyDown(InputKey key);

		// Token: 0x060000E5 RID: 229
		bool IsKeyDownImmediate(InputKey key);

		// Token: 0x060000E6 RID: 230
		bool IsKeyReleased(InputKey key);

		// Token: 0x060000E7 RID: 231
		Vec2 GetResolution();

		// Token: 0x060000E8 RID: 232
		Vec2 GetDesktopResolution();

		// Token: 0x060000E9 RID: 233
		void SetCursorPosition(int x, int y);

		// Token: 0x060000EA RID: 234
		void SetCursorFriction(float frictionValue);

		// Token: 0x060000EB RID: 235
		InputKey[] GetClickKeys();

		// Token: 0x060000EC RID: 236
		void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements);

		// Token: 0x060000ED RID: 237
		void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength);

		// Token: 0x060000EE RID: 238
		void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength);

		// Token: 0x060000EF RID: 239
		void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements);

		// Token: 0x060000F0 RID: 240
		void SetLightbarColor(float red, float green, float blue);
	}
}
