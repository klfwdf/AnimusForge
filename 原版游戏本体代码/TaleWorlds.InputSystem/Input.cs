using System;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x0200000B RID: 11
	public static class Input
	{
		// Token: 0x060000F1 RID: 241 RVA: 0x000037F9 File Offset: 0x000019F9
		public static bool IsPlaystation(this Input.ControllerTypes controllerType)
		{
			return controllerType.HasAnyFlag((Input.ControllerTypes)6);
		}

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x060000F2 RID: 242 RVA: 0x00003802 File Offset: 0x00001A02
		// (set) Token: 0x060000F3 RID: 243 RVA: 0x00003809 File Offset: 0x00001A09
		public static InputState InputState { get; private set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x060000F4 RID: 244 RVA: 0x00003811 File Offset: 0x00001A11
		// (set) Token: 0x060000F5 RID: 245 RVA: 0x00003818 File Offset: 0x00001A18
		public static IInputContext DebugInput { get; private set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x060000F6 RID: 246 RVA: 0x00003820 File Offset: 0x00001A20
		public static IInputManager InputManager
		{
			get
			{
				if (Input.IsOnScreenKeyboardActive)
				{
					return Input._emptyInputManager;
				}
				return Input._inputManager;
			}
		}

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x060000F7 RID: 247 RVA: 0x00003834 File Offset: 0x00001A34
		public static Vec2 Resolution
		{
			get
			{
				return Input._inputManager.GetResolution();
			}
		}

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x060000F8 RID: 248 RVA: 0x00003840 File Offset: 0x00001A40
		public static Vec2 DesktopResolution
		{
			get
			{
				return Input._inputManager.GetDesktopResolution();
			}
		}

		// Token: 0x060000F9 RID: 249 RVA: 0x0000384C File Offset: 0x00001A4C
		public static void Initialize(IInputManager inputManager, IInputContext debugInput)
		{
			Input._emptyInputManager = new EmptyInputManager();
			Input._inputManager = inputManager;
			Input.InputState = new InputState();
			Input.keyData = new byte[256];
			Input.DebugInput = new EmptyInputContext();
		}

		// Token: 0x060000FA RID: 250 RVA: 0x00003881 File Offset: 0x00001A81
		public static void UpdateKeyData(byte[] keyData)
		{
			Input.InputManager.UpdateKeyData(keyData);
		}

		// Token: 0x060000FB RID: 251 RVA: 0x0000388E File Offset: 0x00001A8E
		public static float GetMouseMoveX()
		{
			return Input.InputManager.GetMouseMoveX();
		}

		// Token: 0x060000FC RID: 252 RVA: 0x0000389A File Offset: 0x00001A9A
		public static float GetMouseMoveY()
		{
			return Input.InputManager.GetMouseMoveY();
		}

		// Token: 0x060000FD RID: 253 RVA: 0x000038A6 File Offset: 0x00001AA6
		public static float GetNormalizedMouseMoveX()
		{
			return Input.InputManager.GetNormalizedMouseMoveX();
		}

		// Token: 0x060000FE RID: 254 RVA: 0x000038B2 File Offset: 0x00001AB2
		public static float GetNormalizedMouseMoveY()
		{
			return Input.InputManager.GetNormalizedMouseMoveY();
		}

		// Token: 0x060000FF RID: 255 RVA: 0x000038BE File Offset: 0x00001ABE
		public static float GetGyroX()
		{
			return Input.InputManager.GetGyroX();
		}

		// Token: 0x06000100 RID: 256 RVA: 0x000038CA File Offset: 0x00001ACA
		public static float GetGyroY()
		{
			return Input.InputManager.GetGyroY();
		}

		// Token: 0x06000101 RID: 257 RVA: 0x000038D6 File Offset: 0x00001AD6
		public static float GetGyroZ()
		{
			return Input.InputManager.GetGyroZ();
		}

		// Token: 0x06000102 RID: 258 RVA: 0x000038E2 File Offset: 0x00001AE2
		public static Vec2 GetKeyState(InputKey key)
		{
			return Input.InputManager.GetKeyState(key);
		}

		// Token: 0x06000103 RID: 259 RVA: 0x000038EF File Offset: 0x00001AEF
		public static bool IsKeyPressed(InputKey key)
		{
			return Input.InputManager.IsKeyPressed(key);
		}

		// Token: 0x06000104 RID: 260 RVA: 0x000038FC File Offset: 0x00001AFC
		public static bool IsKeyDown(InputKey key)
		{
			return Input.InputManager.IsKeyDown(key);
		}

		// Token: 0x06000105 RID: 261 RVA: 0x00003909 File Offset: 0x00001B09
		public static bool IsKeyDownImmediate(InputKey key)
		{
			return Input.InputManager.IsKeyDownImmediate(key);
		}

		// Token: 0x06000106 RID: 262 RVA: 0x00003916 File Offset: 0x00001B16
		public static bool IsKeyReleased(InputKey key)
		{
			return Input.InputManager.IsKeyReleased(key);
		}

		// Token: 0x06000107 RID: 263 RVA: 0x00003923 File Offset: 0x00001B23
		public static bool IsControlOrShiftNotDown()
		{
			return !InputKey.LeftControl.IsDown() && !InputKey.RightControl.IsDown() && !InputKey.LeftShift.IsDown() && !InputKey.RightShift.IsDown();
		}

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000108 RID: 264 RVA: 0x0000394F File Offset: 0x00001B4F
		// (set) Token: 0x06000109 RID: 265 RVA: 0x00003956 File Offset: 0x00001B56
		public static bool IsOnScreenKeyboardActive { get; set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600010A RID: 266 RVA: 0x0000395E File Offset: 0x00001B5E
		public static bool IsMouseActive
		{
			get
			{
				return Input.InputManager.IsMouseActive();
			}
		}

		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600010B RID: 267 RVA: 0x0000396A File Offset: 0x00001B6A
		public static bool IsControllerConnected
		{
			get
			{
				return Input.InputManager.IsControllerConnected();
			}
		}

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600010C RID: 268 RVA: 0x00003976 File Offset: 0x00001B76
		// (set) Token: 0x0600010D RID: 269 RVA: 0x0000397D File Offset: 0x00001B7D
		public static bool IsGamepadActive
		{
			get
			{
				return Input._isGamepadActive;
			}
			private set
			{
				if (value != Input._isGamepadActive)
				{
					Input._isGamepadActive = value;
					Action onGamepadActiveStateChanged = Input.OnGamepadActiveStateChanged;
					if (onGamepadActiveStateChanged == null)
					{
						return;
					}
					onGamepadActiveStateChanged();
				}
			}
		}

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x0600010E RID: 270 RVA: 0x0000399C File Offset: 0x00001B9C
		// (set) Token: 0x0600010F RID: 271 RVA: 0x000039A3 File Offset: 0x00001BA3
		public static bool IsAnyTouchActive
		{
			get
			{
				return Input._isAnyTouchActive;
			}
			private set
			{
				if (value != Input._isAnyTouchActive)
				{
					Input._isAnyTouchActive = value;
				}
			}
		}

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000110 RID: 272 RVA: 0x000039B3 File Offset: 0x00001BB3
		// (set) Token: 0x06000111 RID: 273 RVA: 0x000039BA File Offset: 0x00001BBA
		public static Input.ControllerTypes ControllerType
		{
			get
			{
				return Input._controllerType;
			}
			private set
			{
				if (value != Input._controllerType)
				{
					Input._controllerType = value;
					Action<Input.ControllerTypes> onControllerTypeChanged = Input.OnControllerTypeChanged;
					if (onControllerTypeChanged == null)
					{
						return;
					}
					onControllerTypeChanged(value);
				}
			}
		}

		// Token: 0x06000112 RID: 274 RVA: 0x000039DA File Offset: 0x00001BDA
		public static Input.ControllerTypes GetPrimaryControllerType()
		{
			return Input.ControllerTypes.Xbox;
		}

		// Token: 0x06000113 RID: 275 RVA: 0x000039E0 File Offset: 0x00001BE0
		public static int GetFirstKeyPressedInRange(int startKeyNo)
		{
			int result = -1;
			for (int i = startKeyNo; i < 256; i++)
			{
				if (Input.IsKeyPressed((InputKey)i))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000114 RID: 276 RVA: 0x00003A0C File Offset: 0x00001C0C
		public static int GetFirstKeyDownInRange(int startKeyNo)
		{
			int result = -1;
			for (int i = startKeyNo; i < 256; i++)
			{
				if (Input.IsKeyDown((InputKey)i))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000115 RID: 277 RVA: 0x00003A38 File Offset: 0x00001C38
		public static int GetFirstKeyReleasedInRange(int startKeyNo)
		{
			int result = -1;
			for (int i = startKeyNo; i < 256; i++)
			{
				if (Input.IsKeyReleased((InputKey)i))
				{
					result = i;
					break;
				}
			}
			return result;
		}

		// Token: 0x06000116 RID: 278 RVA: 0x00003A64 File Offset: 0x00001C64
		public static void PressKey(InputKey key)
		{
			Input.InputManager.PressKey(key);
		}

		// Token: 0x06000117 RID: 279 RVA: 0x00003A71 File Offset: 0x00001C71
		public static void ClearKeys()
		{
			Input.InputManager.ClearKeys();
		}

		// Token: 0x06000118 RID: 280 RVA: 0x00003A7D File Offset: 0x00001C7D
		public static int GetVirtualKeyCode(InputKey key)
		{
			return Input.InputManager.GetVirtualKeyCode(key);
		}

		// Token: 0x06000119 RID: 281 RVA: 0x00003A8A File Offset: 0x00001C8A
		public static bool IsDown(this InputKey key)
		{
			return Input.IsKeyDown(key);
		}

		// Token: 0x0600011A RID: 282 RVA: 0x00003A92 File Offset: 0x00001C92
		public static bool IsPressed(this InputKey key)
		{
			return Input.IsKeyPressed(key);
		}

		// Token: 0x0600011B RID: 283 RVA: 0x00003A9A File Offset: 0x00001C9A
		public static bool IsReleased(this InputKey key)
		{
			return Input.IsKeyReleased(key);
		}

		// Token: 0x0600011C RID: 284 RVA: 0x00003AA2 File Offset: 0x00001CA2
		public static void SetClipboardText(string text)
		{
			Input.InputManager.SetClipboardText(text);
		}

		// Token: 0x0600011D RID: 285 RVA: 0x00003AAF File Offset: 0x00001CAF
		public static string GetClipboardText()
		{
			return Input.InputManager.GetClipboardText();
		}

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x0600011E RID: 286 RVA: 0x00003ABB File Offset: 0x00001CBB
		public static float MouseMoveX
		{
			get
			{
				return Input.InputManager.GetMouseMoveX();
			}
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x0600011F RID: 287 RVA: 0x00003AC7 File Offset: 0x00001CC7
		public static float MouseMoveY
		{
			get
			{
				return Input.InputManager.GetMouseMoveY();
			}
		}

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000120 RID: 288 RVA: 0x00003AD3 File Offset: 0x00001CD3
		public static float GyroX
		{
			get
			{
				return Input.InputManager.GetGyroX();
			}
		}

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000121 RID: 289 RVA: 0x00003ADF File Offset: 0x00001CDF
		public static float GyroY
		{
			get
			{
				return Input.InputManager.GetGyroY();
			}
		}

		// Token: 0x17000027 RID: 39
		// (get) Token: 0x06000122 RID: 290 RVA: 0x00003AEB File Offset: 0x00001CEB
		public static float GyroZ
		{
			get
			{
				return Input.InputManager.GetGyroZ();
			}
		}

		// Token: 0x17000028 RID: 40
		// (get) Token: 0x06000123 RID: 291 RVA: 0x00003AF7 File Offset: 0x00001CF7
		public static float MouseSensitivity
		{
			get
			{
				return Input.InputManager.GetMouseSensitivity();
			}
		}

		// Token: 0x17000029 RID: 41
		// (get) Token: 0x06000124 RID: 292 RVA: 0x00003B03 File Offset: 0x00001D03
		public static float DeltaMouseScroll
		{
			get
			{
				return Input.InputManager.GetMouseDeltaZ();
			}
		}

		// Token: 0x1700002A RID: 42
		// (get) Token: 0x06000125 RID: 293 RVA: 0x00003B0F File Offset: 0x00001D0F
		public static Vec2 MousePositionRanged
		{
			get
			{
				return Input.InputState.MousePositionRanged;
			}
		}

		// Token: 0x1700002B RID: 43
		// (get) Token: 0x06000126 RID: 294 RVA: 0x00003B1B File Offset: 0x00001D1B
		public static Vec2 MousePositionPixel
		{
			get
			{
				return Input.InputState.MousePositionPixel;
			}
		}

		// Token: 0x06000127 RID: 295 RVA: 0x00003B28 File Offset: 0x00001D28
		public static void Update()
		{
			if (Input.IsOnScreenKeyboardActive)
			{
				return;
			}
			float mousePositionX = Input.InputManager.GetMousePositionX();
			float mousePositionY = Input.InputManager.GetMousePositionY();
			float mouseScrollValue = Input.InputManager.GetMouseScrollValue();
			Input.IsMousePositionUpdated = Input.InputState.UpdateMousePosition(mousePositionX, mousePositionY);
			Input.IsMouseScrollChanged = Input.InputState.UpdateMouseScroll(mouseScrollValue);
			Input.IsGamepadActive = Input.IsControllerConnected && !Input.IsMouseActive;
			Input.IsAnyTouchActive = Input.InputManager.IsAnyTouchActive();
			Input.ControllerType = Input.InputManager.GetControllerType();
			Input.UpdateKeyData(Input.keyData);
		}

		// Token: 0x1700002C RID: 44
		// (get) Token: 0x06000128 RID: 296 RVA: 0x00003BBE File Offset: 0x00001DBE
		// (set) Token: 0x06000129 RID: 297 RVA: 0x00003BC5 File Offset: 0x00001DC5
		public static bool IsMousePositionUpdated { get; private set; }

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600012A RID: 298 RVA: 0x00003BCD File Offset: 0x00001DCD
		// (set) Token: 0x0600012B RID: 299 RVA: 0x00003BD4 File Offset: 0x00001DD4
		public static bool IsMouseScrollChanged { get; private set; }

		// Token: 0x0600012C RID: 300 RVA: 0x00003BDC File Offset: 0x00001DDC
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
			}
			return true;
		}

		// Token: 0x0600012D RID: 301 RVA: 0x00003FF2 File Offset: 0x000021F2
		public static void SetMousePosition(int x, int y)
		{
			Input.InputManager.SetCursorPosition(x, y);
		}

		// Token: 0x0600012E RID: 302 RVA: 0x00004000 File Offset: 0x00002200
		public static void SetCursorFriction(float frictionValue)
		{
			Input.InputManager.SetCursorFriction(frictionValue);
		}

		// Token: 0x0600012F RID: 303 RVA: 0x0000400D File Offset: 0x0000220D
		public static InputKey[] GetClickKeys()
		{
			return Input.InputManager.GetClickKeys();
		}

		// Token: 0x06000130 RID: 304 RVA: 0x00004019 File Offset: 0x00002219
		public static void SetRumbleEffect(float[] lowFrequencyLevels, float[] lowFrequencyDurations, int numLowFrequencyElements, float[] highFrequencyLevels, float[] highFrequencyDurations, int numHighFrequencyElements)
		{
			Input.InputManager.SetRumbleEffect(lowFrequencyLevels, lowFrequencyDurations, numLowFrequencyElements, highFrequencyLevels, highFrequencyDurations, numHighFrequencyElements);
		}

		// Token: 0x06000131 RID: 305 RVA: 0x0000402D File Offset: 0x0000222D
		public static void SetTriggerFeedback(byte leftTriggerPosition, byte leftTriggerStrength, byte rightTriggerPosition, byte rightTriggerStrength)
		{
			Input.InputManager.SetTriggerFeedback(leftTriggerPosition, leftTriggerStrength, rightTriggerPosition, rightTriggerStrength);
		}

		// Token: 0x06000132 RID: 306 RVA: 0x0000403D File Offset: 0x0000223D
		public static void SetTriggerWeaponEffect(byte leftStartPosition, byte leftEnd_position, byte leftStrength, byte rightStartPosition, byte rightEndPosition, byte rightStrength)
		{
			Input.InputManager.SetTriggerWeaponEffect(leftStartPosition, leftEnd_position, leftStrength, rightStartPosition, rightEndPosition, rightStrength);
		}

		// Token: 0x06000133 RID: 307 RVA: 0x00004054 File Offset: 0x00002254
		public static void SetTriggerVibration(float[] leftTriggerAmplitudes, float[] leftTriggerFrequencies, float[] leftTriggerDurations, int numLeftTriggerElements, float[] rightTriggerAmplitudes, float[] rightTriggerFrequencies, float[] rightTriggerDurations, int numRightTriggerElements)
		{
			Input.InputManager.SetTriggerVibration(leftTriggerAmplitudes, leftTriggerFrequencies, leftTriggerDurations, numLeftTriggerElements, rightTriggerAmplitudes, rightTriggerFrequencies, rightTriggerDurations, numRightTriggerElements);
		}

		// Token: 0x06000134 RID: 308 RVA: 0x00004077 File Offset: 0x00002277
		public static void SetLightbarColor(float red, float green, float blue)
		{
			Input.InputManager.SetLightbarColor(red, green, blue);
		}

		// Token: 0x04000027 RID: 39
		public const int NumberOfKeys = 256;

		// Token: 0x04000028 RID: 40
		private static byte[] keyData;

		// Token: 0x04000029 RID: 41
		private static IInputManager _emptyInputManager;

		// Token: 0x0400002A RID: 42
		private static IInputManager _inputManager;

		// Token: 0x0400002C RID: 44
		public static Action OnGamepadActiveStateChanged;

		// Token: 0x0400002D RID: 45
		private static bool _isGamepadActive;

		// Token: 0x0400002E RID: 46
		private static bool _isAnyTouchActive;

		// Token: 0x0400002F RID: 47
		public static Action<Input.ControllerTypes> OnControllerTypeChanged;

		// Token: 0x04000030 RID: 48
		private static Input.ControllerTypes _controllerType;

		// Token: 0x02000017 RID: 23
		public enum ControllerTypes
		{
			// Token: 0x04000166 RID: 358
			None,
			// Token: 0x04000167 RID: 359
			Xbox,
			// Token: 0x04000168 RID: 360
			PlayStationDualShock,
			// Token: 0x04000169 RID: 361
			PlayStationDualSense = 4
		}
	}
}
