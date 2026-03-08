using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000003 RID: 3
	internal class EmptyInputManager : IInputManager
	{
		// Token: 0x06000020 RID: 32 RVA: 0x00002142 File Offset: 0x00000342
		public void ClearKeys()
		{
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002144 File Offset: 0x00000344
		public InputKey[] GetClickKeys()
		{
			return new InputKey[0];
		}

		// Token: 0x06000022 RID: 34 RVA: 0x0000214C File Offset: 0x0000034C
		public string GetClipboardText()
		{
			return string.Empty;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x00002153 File Offset: 0x00000353
		public Input.ControllerTypes GetControllerType()
		{
			return Input.ControllerTypes.None;
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002156 File Offset: 0x00000356
		public Vec2 GetDesktopResolution()
		{
			return Vec2.Zero;
		}

		// Token: 0x06000025 RID: 37 RVA: 0x0000215D File Offset: 0x0000035D
		public float GetGyroX()
		{
			return 0f;
		}

		// Token: 0x06000026 RID: 38 RVA: 0x00002164 File Offset: 0x00000364
		public float GetGyroY()
		{
			return 0f;
		}

		// Token: 0x06000027 RID: 39 RVA: 0x0000216B File Offset: 0x0000036B
		public float GetGyroZ()
		{
			return 0f;
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002172 File Offset: 0x00000372
		public Vec2 GetKeyState(InputKey key)
		{
			return Vec2.Zero;
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002179 File Offset: 0x00000379
		public float GetMouseDeltaZ()
		{
			return 0f;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x00002180 File Offset: 0x00000380
		public float GetMouseMoveX()
		{
			return 0f;
		}

		// Token: 0x0600002B RID: 43 RVA: 0x00002187 File Offset: 0x00000387
		public float GetMouseMoveY()
		{
			return 0f;
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000218E File Offset: 0x0000038E
		public float GetMousePositionX()
		{
			return 0f;
		}

		// Token: 0x0600002D RID: 45 RVA: 0x00002195 File Offset: 0x00000395
		public float GetMousePositionY()
		{
			return 0f;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x0000219C File Offset: 0x0000039C
		public float GetMouseScrollValue()
		{
			return 0f;
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000021A3 File Offset: 0x000003A3
		public float GetMouseSensitivity()
		{
			return 0f;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x000021AA File Offset: 0x000003AA
		public float GetNormalizedMouseMoveX()
		{
			return 0f;
		}

		// Token: 0x06000031 RID: 49 RVA: 0x000021B1 File Offset: 0x000003B1
		public float GetNormalizedMouseMoveY()
		{
			return 0f;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000021B8 File Offset: 0x000003B8
		public Vec2 GetResolution()
		{
			return Vec2.Zero;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x000021BF File Offset: 0x000003BF
		public int GetVirtualKeyCode(InputKey key)
		{
			return -1;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x000021C2 File Offset: 0x000003C2
		public bool IsAnyTouchActive()
		{
			return false;
		}

		// Token: 0x06000035 RID: 53 RVA: 0x000021C5 File Offset: 0x000003C5
		public bool IsControllerConnected()
		{
			return false;
		}

		// Token: 0x06000036 RID: 54 RVA: 0x000021C8 File Offset: 0x000003C8
		public bool IsKeyDown(InputKey key)
		{
			return false;
		}

		// Token: 0x06000037 RID: 55 RVA: 0x000021CB File Offset: 0x000003CB
		public bool IsKeyDownImmediate(InputKey key)
		{
			return false;
		}

		// Token: 0x06000038 RID: 56 RVA: 0x000021CE File Offset: 0x000003CE
		public bool IsKeyPressed(InputKey key)
		{
			return false;
		}

		// Token: 0x06000039 RID: 57 RVA: 0x000021D1 File Offset: 0x000003D1
		public bool IsKeyReleased(InputKey key)
		{
			return false;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x000021D4 File Offset: 0x000003D4
		public bool IsMouseActive()
		{
			return false;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x000021D7 File Offset: 0x000003D7
		public void PressKey(InputKey key)
		{
		}

		// Token: 0x0600003C RID: 60 RVA: 0x000021D9 File Offset: 0x000003D9
		public void SetClipboardText(string text)
		{
		}

		// Token: 0x0600003D RID: 61 RVA: 0x000021DB File Offset: 0x000003DB
		public void SetCursorFriction(float frictionValue)
		{
		}

		// Token: 0x0600003E RID: 62 RVA: 0x000021DD File Offset: 0x000003DD
		public void SetCursorPosition(int x, int y)
		{
		}

		// Token: 0x0600003F RID: 63 RVA: 0x000021DF File Offset: 0x000003DF
		public void SetLightbarColor(float red, float green, float blue)
		{
		}

		// Token: 0x06000040 RID: 64 RVA: 0x000021E1 File Offset: 0x000003E1
		public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
		{
		}

		// Token: 0x06000041 RID: 65 RVA: 0x000021E3 File Offset: 0x000003E3
		public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
		{
		}

		// Token: 0x06000042 RID: 66 RVA: 0x000021E5 File Offset: 0x000003E5
		public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
		{
		}

		// Token: 0x06000043 RID: 67 RVA: 0x000021E7 File Offset: 0x000003E7
		public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
		{
		}

		// Token: 0x06000044 RID: 68 RVA: 0x000021E9 File Offset: 0x000003E9
		public void UpdateKeyData(byte[] keyData)
		{
		}
	}
}
