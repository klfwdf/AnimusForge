using System;
using System.Drawing;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension.Standalone.Native.Windows;

namespace TaleWorlds.TwoDimension.Standalone
{
	// Token: 0x0200000C RID: 12
	public class StandaloneInputManager : IInputManager
	{
		// Token: 0x06000084 RID: 132 RVA: 0x000047CD File Offset: 0x000029CD
		public StandaloneInputManager(GraphicsForm graphicsForm)
		{
			this._graphicsForm = graphicsForm;
		}

		// Token: 0x06000085 RID: 133 RVA: 0x000047DC File Offset: 0x000029DC
		float IInputManager.GetMousePositionX()
		{
			return this._graphicsForm.MousePosition().X / (float)this._graphicsForm.Width;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x000047FB File Offset: 0x000029FB
		float IInputManager.GetMousePositionY()
		{
			return this._graphicsForm.MousePosition().Y / (float)this._graphicsForm.Height;
		}

		// Token: 0x06000087 RID: 135 RVA: 0x0000481A File Offset: 0x00002A1A
		float IInputManager.GetMouseScrollValue()
		{
			return 0f;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00004821 File Offset: 0x00002A21
		bool IInputManager.IsMouseActive()
		{
			return true;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x00004824 File Offset: 0x00002A24
		bool IInputManager.IsAnyTouchActive()
		{
			return false;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00004827 File Offset: 0x00002A27
		bool IInputManager.IsControllerConnected()
		{
			return false;
		}

		// Token: 0x0600008B RID: 139 RVA: 0x0000482A File Offset: 0x00002A2A
		void IInputManager.PressKey(InputKey key)
		{
		}

		// Token: 0x0600008C RID: 140 RVA: 0x0000482C File Offset: 0x00002A2C
		void IInputManager.ClearKeys()
		{
		}

		// Token: 0x0600008D RID: 141 RVA: 0x0000482E File Offset: 0x00002A2E
		int IInputManager.GetVirtualKeyCode(InputKey key)
		{
			return -1;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00004831 File Offset: 0x00002A31
		void IInputManager.SetClipboardText(string text)
		{
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00004833 File Offset: 0x00002A33
		string IInputManager.GetClipboardText()
		{
			return "";
		}

		// Token: 0x06000090 RID: 144 RVA: 0x0000483A File Offset: 0x00002A3A
		float IInputManager.GetMouseMoveX()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00004841 File Offset: 0x00002A41
		float IInputManager.GetMouseMoveY()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00004848 File Offset: 0x00002A48
		float IInputManager.GetNormalizedMouseMoveX()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000093 RID: 147 RVA: 0x0000484F File Offset: 0x00002A4F
		float IInputManager.GetNormalizedMouseMoveY()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00004856 File Offset: 0x00002A56
		float IInputManager.GetGyroX()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000095 RID: 149 RVA: 0x0000485D File Offset: 0x00002A5D
		float IInputManager.GetGyroY()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00004864 File Offset: 0x00002A64
		float IInputManager.GetGyroZ()
		{
			throw new NotImplementedException();
		}

		// Token: 0x06000097 RID: 151 RVA: 0x0000486B File Offset: 0x00002A6B
		float IInputManager.GetMouseSensitivity()
		{
			return 1f;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00004872 File Offset: 0x00002A72
		float IInputManager.GetMouseDeltaZ()
		{
			return this._graphicsForm.GetMouseDeltaZ();
		}

		// Token: 0x06000099 RID: 153 RVA: 0x0000487F File Offset: 0x00002A7F
		void IInputManager.UpdateKeyData(byte[] keyData)
		{
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00004881 File Offset: 0x00002A81
		Vec2 IInputManager.GetKeyState(InputKey key)
		{
			if (!this._graphicsForm.GetKey(key))
			{
				return new Vec2(0f, 0f);
			}
			return new Vec2(1f, 0f);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x000048B0 File Offset: 0x00002AB0
		bool IInputManager.IsKeyPressed(InputKey key)
		{
			return this._graphicsForm.GetKeyDown(key);
		}

		// Token: 0x0600009C RID: 156 RVA: 0x000048BE File Offset: 0x00002ABE
		bool IInputManager.IsKeyDown(InputKey key)
		{
			return this._graphicsForm.GetKey(key);
		}

		// Token: 0x0600009D RID: 157 RVA: 0x000048CC File Offset: 0x00002ACC
		bool IInputManager.IsKeyDownImmediate(InputKey key)
		{
			return this._graphicsForm.GetKey(key);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x000048DA File Offset: 0x00002ADA
		bool IInputManager.IsKeyReleased(InputKey key)
		{
			return this._graphicsForm.GetKeyUp(key);
		}

		// Token: 0x0600009F RID: 159 RVA: 0x000048E8 File Offset: 0x00002AE8
		Vec2 IInputManager.GetResolution()
		{
			return new Vec2((float)this._graphicsForm.Width, (float)this._graphicsForm.Height);
		}

		// Token: 0x060000A0 RID: 160 RVA: 0x00004908 File Offset: 0x00002B08
		Vec2 IInputManager.GetDesktopResolution()
		{
			Rectangle rectangle;
			User32.GetClientRect(User32.GetDesktopWindow(), out rectangle);
			return new Vec2((float)rectangle.Width, (float)rectangle.Height);
		}

		// Token: 0x060000A1 RID: 161 RVA: 0x00004937 File Offset: 0x00002B37
		void IInputManager.SetCursorPosition(int x, int y)
		{
		}

		// Token: 0x060000A2 RID: 162 RVA: 0x00004939 File Offset: 0x00002B39
		void IInputManager.SetCursorFriction(float frictionValue)
		{
		}

		// Token: 0x060000A3 RID: 163 RVA: 0x0000493B File Offset: 0x00002B3B
		InputKey[] IInputManager.GetClickKeys()
		{
			return new InputKey[]
			{
				InputKey.LeftMouseButton,
				InputKey.ControllerRDown
			};
		}

		// Token: 0x060000A4 RID: 164 RVA: 0x00004953 File Offset: 0x00002B53
		public void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
		{
		}

		// Token: 0x060000A5 RID: 165 RVA: 0x00004955 File Offset: 0x00002B55
		public void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
		{
		}

		// Token: 0x060000A6 RID: 166 RVA: 0x00004957 File Offset: 0x00002B57
		public void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
		{
		}

		// Token: 0x060000A7 RID: 167 RVA: 0x00004959 File Offset: 0x00002B59
		public void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
		{
		}

		// Token: 0x060000A8 RID: 168 RVA: 0x0000495B File Offset: 0x00002B5B
		public void SetLightbarColor(float red, float green, float blue)
		{
		}

		// Token: 0x060000A9 RID: 169 RVA: 0x0000495D File Offset: 0x00002B5D
		Input.ControllerTypes IInputManager.GetControllerType()
		{
			return Input.ControllerTypes.Xbox;
		}

		// Token: 0x0400003F RID: 63
		private GraphicsForm _graphicsForm;
	}
}
