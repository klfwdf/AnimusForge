using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000010 RID: 16
	[Serializable]
	public class Key
	{
		// Token: 0x1700003B RID: 59
		// (get) Token: 0x06000186 RID: 390 RVA: 0x0000518F File Offset: 0x0000338F
		// (set) Token: 0x06000187 RID: 391 RVA: 0x00005197 File Offset: 0x00003397
		public bool IsKeyboardInput { get; private set; }

		// Token: 0x1700003C RID: 60
		// (get) Token: 0x06000188 RID: 392 RVA: 0x000051A0 File Offset: 0x000033A0
		// (set) Token: 0x06000189 RID: 393 RVA: 0x000051A8 File Offset: 0x000033A8
		public bool IsMouseButtonInput { get; private set; }

		// Token: 0x1700003D RID: 61
		// (get) Token: 0x0600018A RID: 394 RVA: 0x000051B1 File Offset: 0x000033B1
		// (set) Token: 0x0600018B RID: 395 RVA: 0x000051B9 File Offset: 0x000033B9
		public bool IsMouseWheelInput { get; private set; }

		// Token: 0x1700003E RID: 62
		// (get) Token: 0x0600018C RID: 396 RVA: 0x000051C2 File Offset: 0x000033C2
		// (set) Token: 0x0600018D RID: 397 RVA: 0x000051CA File Offset: 0x000033CA
		public bool IsControllerInput { get; private set; }

		// Token: 0x1700003F RID: 63
		// (get) Token: 0x0600018E RID: 398 RVA: 0x000051D3 File Offset: 0x000033D3
		// (set) Token: 0x0600018F RID: 399 RVA: 0x000051DB File Offset: 0x000033DB
		public InputKey InputKey { get; private set; }

		// Token: 0x06000190 RID: 400 RVA: 0x000051E4 File Offset: 0x000033E4
		public Key(InputKey key)
		{
			this.ChangeKey(key);
		}

		// Token: 0x06000191 RID: 401 RVA: 0x000051F3 File Offset: 0x000033F3
		public Key()
		{
		}

		// Token: 0x06000192 RID: 402 RVA: 0x000051FC File Offset: 0x000033FC
		public void ChangeKey(InputKey key)
		{
			this.InputKey = key;
			this.IsKeyboardInput = Key.GetInputType(key) == Key.InputType.Keyboard;
			this.IsMouseButtonInput = Key.GetInputType(key) == Key.InputType.MouseButton;
			this.IsMouseWheelInput = Key.GetInputType(key) == Key.InputType.MouseWheel;
			this.IsControllerInput = Key.GetInputType(key) == Key.InputType.Controller;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x0000524C File Offset: 0x0000344C
		internal bool IsPressed()
		{
			return Input.IsKeyPressed(this.InputKey);
		}

		// Token: 0x06000194 RID: 404 RVA: 0x00005259 File Offset: 0x00003459
		internal bool IsDown()
		{
			return Input.IsKeyDown(this.InputKey);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x00005266 File Offset: 0x00003466
		internal bool IsDownImmediate()
		{
			if (this.IsKeyboardInput || this.IsMouseButtonInput)
			{
				return Input.IsKeyDownImmediate(this.InputKey);
			}
			return Input.IsKeyDown(this.InputKey);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000528F File Offset: 0x0000348F
		internal bool IsReleased()
		{
			return Input.IsKeyReleased(this.InputKey);
		}

		// Token: 0x06000197 RID: 407 RVA: 0x0000529C File Offset: 0x0000349C
		internal Vec2 GetKeyState()
		{
			return Input.GetKeyState(this.InputKey);
		}

		// Token: 0x06000198 RID: 408 RVA: 0x000052AC File Offset: 0x000034AC
		public override string ToString()
		{
			if (this.IsKeyboardInput)
			{
				int virtualKeyCode = Input.GetVirtualKeyCode(this.InputKey);
				if (virtualKeyCode != 0)
				{
					VirtualKeyCode virtualKeyCode2 = (VirtualKeyCode)virtualKeyCode;
					return virtualKeyCode2.ToString();
				}
			}
			return this.InputKey.ToString();
		}

		// Token: 0x06000199 RID: 409 RVA: 0x000052F4 File Offset: 0x000034F4
		public override bool Equals(object obj)
		{
			return (obj as Key).InputKey == this.InputKey;
		}

		// Token: 0x0600019A RID: 410 RVA: 0x00005309 File Offset: 0x00003509
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x0600019B RID: 411 RVA: 0x00005311 File Offset: 0x00003511
		public static bool operator ==(Key k1, Key k2)
		{
			return k1 == k2 || (k1 != null && k2 != null && k1.InputKey == k2.InputKey);
		}

		// Token: 0x0600019C RID: 412 RVA: 0x0000532F File Offset: 0x0000352F
		public static bool operator !=(Key k1, Key k2)
		{
			return !(k1 == k2);
		}

		// Token: 0x0600019D RID: 413 RVA: 0x0000533B File Offset: 0x0000353B
		public static bool IsLeftAnalogInput(InputKey key)
		{
			return key == InputKey.ControllerLStick || key - InputKey.ControllerLStickUp <= 3 || key == InputKey.ControllerLThumb;
		}

		// Token: 0x0600019E RID: 414 RVA: 0x0000535A File Offset: 0x0000355A
		public static bool IsLeftBumperOrTriggerInput(InputKey key)
		{
			return key == InputKey.ControllerLBumper || key == InputKey.ControllerLTrigger;
		}

		// Token: 0x0600019F RID: 415 RVA: 0x0000536F File Offset: 0x0000356F
		public static bool IsRightBumperOrTriggerInput(InputKey key)
		{
			return key == InputKey.ControllerRBumper || key == InputKey.ControllerRTrigger;
		}

		// Token: 0x060001A0 RID: 416 RVA: 0x00005384 File Offset: 0x00003584
		public static bool IsFaceKeyInput(InputKey key)
		{
			return key - InputKey.ControllerRUp <= 3;
		}

		// Token: 0x060001A1 RID: 417 RVA: 0x00005393 File Offset: 0x00003593
		public static bool IsRightAnalogInput(InputKey key)
		{
			return key == InputKey.ControllerRStick || key - InputKey.ControllerRStickUp <= 3 || key == InputKey.ControllerRThumb;
		}

		// Token: 0x060001A2 RID: 418 RVA: 0x000053B2 File Offset: 0x000035B2
		public static bool IsDpadInput(InputKey key)
		{
			return key - InputKey.ControllerLUp <= 3;
		}

		// Token: 0x060001A3 RID: 419 RVA: 0x000053C4 File Offset: 0x000035C4
		public static Key.InputType GetInputType(InputKey key)
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
				return Key.InputType.Keyboard;
			case InputKey.ControllerLStick:
			case InputKey.ControllerRStick:
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
				return Key.InputType.Controller;
			case InputKey.LeftMouseButton:
			case InputKey.RightMouseButton:
			case InputKey.MiddleMouseButton:
			case InputKey.X1MouseButton:
			case InputKey.X2MouseButton:
				return Key.InputType.MouseButton;
			case InputKey.MouseScrollUp:
			case InputKey.MouseScrollDown:
				return Key.InputType.MouseWheel;
			}
			return Key.InputType.Invalid;
		}

		// Token: 0x02000018 RID: 24
		public enum InputType
		{
			// Token: 0x0400016B RID: 363
			Invalid = -1,
			// Token: 0x0400016C RID: 364
			Keyboard,
			// Token: 0x0400016D RID: 365
			MouseButton,
			// Token: 0x0400016E RID: 366
			MouseWheel,
			// Token: 0x0400016F RID: 367
			Controller
		}
	}
}
