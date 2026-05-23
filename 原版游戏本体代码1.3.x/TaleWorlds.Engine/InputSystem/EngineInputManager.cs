using System;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.Engine.InputSystem
{
	// Token: 0x020000AF RID: 175
	public class EngineInputManager : IInputManager
	{
		// Token: 0x06000F7B RID: 3963 RVA: 0x00013440 File Offset: 0x00011640
		float IInputManager.GetMousePositionX()
		{
			return EngineApplicationInterface.IInput.GetMousePositionX();
		}

		// Token: 0x06000F7C RID: 3964 RVA: 0x0001344C File Offset: 0x0001164C
		float IInputManager.GetMousePositionY()
		{
			return EngineApplicationInterface.IInput.GetMousePositionY();
		}

		// Token: 0x06000F7D RID: 3965 RVA: 0x00013458 File Offset: 0x00011658
		float IInputManager.GetMouseScrollValue()
		{
			return EngineApplicationInterface.IInput.GetMouseScrollValue();
		}

		// Token: 0x06000F7E RID: 3966 RVA: 0x00013464 File Offset: 0x00011664
		bool IInputManager.IsMouseActive()
		{
			return EngineApplicationInterface.IInput.IsMouseActive();
		}

		// Token: 0x06000F7F RID: 3967 RVA: 0x00013470 File Offset: 0x00011670
		bool IInputManager.IsControllerConnected()
		{
			return EngineApplicationInterface.IInput.IsControllerConnected();
		}

		// Token: 0x06000F80 RID: 3968 RVA: 0x0001347C File Offset: 0x0001167C
		void IInputManager.PressKey(InputKey key)
		{
			EngineApplicationInterface.IInput.PressKey(key);
		}

		// Token: 0x06000F81 RID: 3969 RVA: 0x00013489 File Offset: 0x00011689
		void IInputManager.ClearKeys()
		{
			EngineApplicationInterface.IInput.ClearKeys();
		}

		// Token: 0x06000F82 RID: 3970 RVA: 0x00013495 File Offset: 0x00011695
		int IInputManager.GetVirtualKeyCode(InputKey key)
		{
			return EngineApplicationInterface.IInput.GetVirtualKeyCode(key);
		}

		// Token: 0x06000F83 RID: 3971 RVA: 0x000134A2 File Offset: 0x000116A2
		void IInputManager.SetClipboardText(string text)
		{
			EngineApplicationInterface.IInput.SetClipboardText(text);
		}

		// Token: 0x06000F84 RID: 3972 RVA: 0x000134AF File Offset: 0x000116AF
		string IInputManager.GetClipboardText()
		{
			return EngineApplicationInterface.IInput.GetClipboardText();
		}

		// Token: 0x06000F85 RID: 3973 RVA: 0x000134BB File Offset: 0x000116BB
		float IInputManager.GetMouseMoveX()
		{
			return EngineApplicationInterface.IInput.GetMouseMoveX();
		}

		// Token: 0x06000F86 RID: 3974 RVA: 0x000134C7 File Offset: 0x000116C7
		float IInputManager.GetMouseMoveY()
		{
			return EngineApplicationInterface.IInput.GetMouseMoveY();
		}

		// Token: 0x06000F87 RID: 3975 RVA: 0x000134D3 File Offset: 0x000116D3
		float IInputManager.GetNormalizedMouseMoveX()
		{
			return EngineApplicationInterface.IInput.GetMouseMoveX() / Screen.RealScreenResolutionWidth;
		}

		// Token: 0x06000F88 RID: 3976 RVA: 0x000134E5 File Offset: 0x000116E5
		float IInputManager.GetNormalizedMouseMoveY()
		{
			return EngineApplicationInterface.IInput.GetMouseMoveY() / Screen.RealScreenResolutionHeight;
		}

		// Token: 0x06000F89 RID: 3977 RVA: 0x000134F7 File Offset: 0x000116F7
		float IInputManager.GetGyroX()
		{
			return EngineApplicationInterface.IInput.GetGyroX();
		}

		// Token: 0x06000F8A RID: 3978 RVA: 0x00013503 File Offset: 0x00011703
		float IInputManager.GetGyroY()
		{
			return EngineApplicationInterface.IInput.GetGyroY();
		}

		// Token: 0x06000F8B RID: 3979 RVA: 0x0001350F File Offset: 0x0001170F
		float IInputManager.GetGyroZ()
		{
			return EngineApplicationInterface.IInput.GetGyroZ();
		}

		// Token: 0x06000F8C RID: 3980 RVA: 0x0001351B File Offset: 0x0001171B
		float IInputManager.GetMouseSensitivity()
		{
			return EngineApplicationInterface.IInput.GetMouseSensitivity();
		}

		// Token: 0x06000F8D RID: 3981 RVA: 0x00013527 File Offset: 0x00011727
		float IInputManager.GetMouseDeltaZ()
		{
			return EngineApplicationInterface.IInput.GetMouseDeltaZ();
		}

		// Token: 0x06000F8E RID: 3982 RVA: 0x00013533 File Offset: 0x00011733
		void IInputManager.UpdateKeyData(byte[] keyData)
		{
			EngineApplicationInterface.IInput.UpdateKeyData(keyData);
		}

		// Token: 0x06000F8F RID: 3983 RVA: 0x00013540 File Offset: 0x00011740
		Vec2 IInputManager.GetKeyState(InputKey key)
		{
			return EngineApplicationInterface.IInput.GetKeyState(key);
		}

		// Token: 0x06000F90 RID: 3984 RVA: 0x0001354D File Offset: 0x0001174D
		bool IInputManager.IsKeyPressed(InputKey key)
		{
			return EngineApplicationInterface.IInput.IsKeyPressed(key);
		}

		// Token: 0x06000F91 RID: 3985 RVA: 0x0001355A File Offset: 0x0001175A
		bool IInputManager.IsKeyDown(InputKey key)
		{
			return EngineApplicationInterface.IInput.IsKeyDown(key);
		}

		// Token: 0x06000F92 RID: 3986 RVA: 0x00013567 File Offset: 0x00011767
		bool IInputManager.IsKeyDownImmediate(InputKey key)
		{
			return EngineApplicationInterface.IInput.IsKeyDownImmediate(key);
		}

		// Token: 0x06000F93 RID: 3987 RVA: 0x00013574 File Offset: 0x00011774
		bool IInputManager.IsKeyReleased(InputKey key)
		{
			return EngineApplicationInterface.IInput.IsKeyReleased(key);
		}

		// Token: 0x06000F94 RID: 3988 RVA: 0x00013581 File Offset: 0x00011781
		Vec2 IInputManager.GetResolution()
		{
			return Screen.RealScreenResolution;
		}

		// Token: 0x06000F95 RID: 3989 RVA: 0x00013588 File Offset: 0x00011788
		Vec2 IInputManager.GetDesktopResolution()
		{
			return Screen.DesktopResolution;
		}

		// Token: 0x06000F96 RID: 3990 RVA: 0x00013590 File Offset: 0x00011790
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

		// Token: 0x06000F97 RID: 3991 RVA: 0x00013605 File Offset: 0x00011805
		void IInputManager.SetCursorFriction(float frictionValue)
		{
			EngineApplicationInterface.IInput.SetCursorFrictionValue(frictionValue);
		}

		// Token: 0x06000F98 RID: 3992 RVA: 0x00013614 File Offset: 0x00011814
		InputKey[] IInputManager.GetClickKeys()
		{
			InputKey inputKey = (EngineApplicationInterface.IScreen.IsEnterButtonCross() ? InputKey.ControllerRDown : InputKey.ControllerRRight);
			if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) != 0f)
			{
				return new InputKey[]
				{
					InputKey.LeftMouseButton,
					inputKey,
					InputKey.ControllerLOptionTap
				};
			}
			return new InputKey[]
			{
				InputKey.LeftMouseButton,
				inputKey
			};
		}

		// Token: 0x06000F99 RID: 3993 RVA: 0x00013675 File Offset: 0x00011875
		public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
		{
			EngineApplicationInterface.IInput.SetRumbleEffect(lowFrequencyLevels, lowFrequencyDurations, numLowFrequencyElements, highFrequencyLevels, highFrequencyDurations, numHighFrequencyElements);
		}

		// Token: 0x06000F9A RID: 3994 RVA: 0x0001368A File Offset: 0x0001188A
		public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
		{
			EngineApplicationInterface.IInput.SetTriggerFeedback(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
		}

		// Token: 0x06000F9B RID: 3995 RVA: 0x0001369B File Offset: 0x0001189B
		public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
		{
			EngineApplicationInterface.IInput.SetTriggerWeaponEffect(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
		}

		// Token: 0x06000F9C RID: 3996 RVA: 0x000136B0 File Offset: 0x000118B0
		public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
		{
			EngineApplicationInterface.IInput.SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, leftTriggerDurations, numLeftTriggerElements, rightTriggerAmplitudes, rightTriggerFrequencies, rightTriggerDurations, numRightTriggerElements);
		}

		// Token: 0x06000F9D RID: 3997 RVA: 0x000136D4 File Offset: 0x000118D4
		public void SetLightbarColor(float red, float green, float blue)
		{
			EngineApplicationInterface.IInput.SetLightbarColor(red, green, blue);
		}

		// Token: 0x06000F9E RID: 3998 RVA: 0x000136E3 File Offset: 0x000118E3
		Input.ControllerTypes IInputManager.GetControllerType()
		{
			return (Input.ControllerTypes)EngineApplicationInterface.IInput.GetControllerType();
		}

		// Token: 0x06000F9F RID: 3999 RVA: 0x000136EF File Offset: 0x000118EF
		bool IInputManager.IsAnyTouchActive()
		{
			return EngineApplicationInterface.IInput.IsAnyTouchActive();
		}
	}
}
