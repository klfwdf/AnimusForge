using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x0200000C RID: 12
	public class InputContext : IInputContext
	{
		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000135 RID: 309 RVA: 0x00004086 File Offset: 0x00002286
		// (set) Token: 0x06000136 RID: 310 RVA: 0x0000408E File Offset: 0x0000228E
		public bool IsKeysAllowed { get; set; }

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000137 RID: 311 RVA: 0x00004097 File Offset: 0x00002297
		// (set) Token: 0x06000138 RID: 312 RVA: 0x0000409F File Offset: 0x0000229F
		public bool IsMouseButtonAllowed { get; set; }

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000139 RID: 313 RVA: 0x000040A8 File Offset: 0x000022A8
		// (set) Token: 0x0600013A RID: 314 RVA: 0x000040B0 File Offset: 0x000022B0
		public bool IsMouseWheelAllowed { get; set; }

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x0600013B RID: 315 RVA: 0x000040B9 File Offset: 0x000022B9
		public bool IsControllerAllowed
		{
			get
			{
				return this.IsKeysAllowed;
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x0600013C RID: 316 RVA: 0x000040C1 File Offset: 0x000022C1
		// (set) Token: 0x0600013D RID: 317 RVA: 0x000040C9 File Offset: 0x000022C9
		public bool MouseOnMe { get; set; }

		// Token: 0x0600013E RID: 318 RVA: 0x000040D4 File Offset: 0x000022D4
		public InputContext()
		{
			this._categories = new List<GameKeyContext>();
			this._registeredGameKeys = new List<GameKey>();
			this._registeredHotKeys = new Dictionary<string, HotKey>();
			this._registeredGameAxisKeys = new Dictionary<string, GameAxisKey>();
			this._downInputKeys = new List<Key>();
			this.MouseOnMe = false;
		}

		// Token: 0x0600013F RID: 319 RVA: 0x00004130 File Offset: 0x00002330
		public int GetPointerX()
		{
			float x = Input.Resolution.x;
			return (int)(this.GetMousePositionRanged().x * x);
		}

		// Token: 0x06000140 RID: 320 RVA: 0x00004158 File Offset: 0x00002358
		public int GetPointerY()
		{
			float y = Input.Resolution.y;
			return (int)(this.GetMousePositionRanged().y * y);
		}

		// Token: 0x06000141 RID: 321 RVA: 0x00004180 File Offset: 0x00002380
		public Vector2 GetPointerPosition()
		{
			Vec2 resolution = Input.Resolution;
			float x = resolution.x;
			float y = resolution.y;
			float num = this.GetMousePositionRanged().x * x;
			float num2 = this.GetMousePositionRanged().y * y;
			return new Vector2(num, num2);
		}

		// Token: 0x06000142 RID: 322 RVA: 0x000041C0 File Offset: 0x000023C0
		public Vec2 GetPointerPositionVec2()
		{
			Vec2 resolution = Input.Resolution;
			float x = resolution.x;
			float y = resolution.y;
			float a = this.GetMousePositionRanged().x * x;
			float b = this.GetMousePositionRanged().y * y;
			return new Vec2(a, b);
		}

		// Token: 0x06000143 RID: 323 RVA: 0x00004200 File Offset: 0x00002400
		public void RegisterHotKeyCategory(GameKeyContext category)
		{
			this._categories.Add(category);
			foreach (HotKey hotKey in category.RegisteredHotKeys)
			{
				if (!this._registeredHotKeys.ContainsKey(hotKey.Id))
				{
					this._registeredHotKeys.Add(hotKey.Id, hotKey);
				}
			}
			if (this._registeredGameKeys.Count == 0)
			{
				int count = category.RegisteredGameKeys.Count;
				for (int i = 0; i < count; i++)
				{
					this._registeredGameKeys.Add(null);
				}
			}
			foreach (GameKey gameKey in category.RegisteredGameKeys)
			{
				if (gameKey != null)
				{
					this._registeredGameKeys[gameKey.Id] = gameKey;
				}
			}
			foreach (GameAxisKey gameAxisKey in category.RegisteredGameAxisKeys)
			{
				if (!this._registeredGameAxisKeys.ContainsKey(gameAxisKey.Id))
				{
					this._registeredGameAxisKeys.Add(gameAxisKey.Id, gameAxisKey);
				}
			}
		}

		// Token: 0x06000144 RID: 324 RVA: 0x00004368 File Offset: 0x00002568
		public bool IsCategoryRegistered(GameKeyContext category)
		{
			List<GameKeyContext> categories = this._categories;
			return categories != null && categories.Contains(category);
		}

		// Token: 0x06000145 RID: 325 RVA: 0x0000437C File Offset: 0x0000257C
		private List<Key> GetAllAvailableKeys()
		{
			this._allKeysListMemoryCache.Clear();
			for (int i = 0; i < this._registeredGameKeys.Count; i++)
			{
				GameKey gameKey = this._registeredGameKeys[i];
				if (gameKey != null)
				{
					if (gameKey.KeyboardKey != null)
					{
						this._allKeysListMemoryCache.Add(gameKey.KeyboardKey);
					}
					if (gameKey.ControllerKey != null)
					{
						this._allKeysListMemoryCache.Add(gameKey.ControllerKey);
					}
				}
			}
			foreach (HotKey hotKey in this._registeredHotKeys.Values)
			{
				for (int j = 0; j < hotKey.Keys.Count; j++)
				{
					if (hotKey.Keys[j] != null)
					{
						this._allKeysListMemoryCache.Add(hotKey.Keys[j]);
					}
				}
			}
			return this._allKeysListMemoryCache;
		}

		// Token: 0x06000146 RID: 326 RVA: 0x0000448C File Offset: 0x0000268C
		public void RegisterDownKeys()
		{
			List<Key> allAvailableKeys = this.GetAllAvailableKeys();
			for (int i = 0; i < allAvailableKeys.Count; i++)
			{
				Key key = allAvailableKeys[i];
				if (key.IsPressed() && !this._downInputKeys.Contains(key))
				{
					this._downInputKeys.Add(key);
				}
			}
		}

		// Token: 0x06000147 RID: 327 RVA: 0x000044DC File Offset: 0x000026DC
		public void UnregisterReleasedKeys()
		{
			List<Key> allAvailableKeys = this.GetAllAvailableKeys();
			for (int i = 0; i < allAvailableKeys.Count; i++)
			{
				Key key = allAvailableKeys[i];
				if (key.IsReleased() && this._downInputKeys.Contains(key))
				{
					this._downInputKeys.Remove(key);
				}
			}
		}

		// Token: 0x06000148 RID: 328 RVA: 0x0000452C File Offset: 0x0000272C
		public void ResetLastDownKeys()
		{
			this._downInputKeys.Clear();
		}

		// Token: 0x06000149 RID: 329 RVA: 0x00004539 File Offset: 0x00002739
		private bool IsHotKeyDown(HotKey hotKey)
		{
			return hotKey.IsDown(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x0600014A RID: 330 RVA: 0x00004564 File Offset: 0x00002764
		public bool IsHotKeyDown(string hotKey)
		{
			HotKey hotKey2;
			return this._registeredHotKeys.TryGetValue(hotKey, out hotKey2) && this.IsHotKeyDown(hotKey2);
		}

		// Token: 0x0600014B RID: 331 RVA: 0x0000458A File Offset: 0x0000278A
		private bool IsGameKeyDown(GameKey gameKey)
		{
			return gameKey.IsDown(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed, true);
		}

		// Token: 0x0600014C RID: 332 RVA: 0x000045B8 File Offset: 0x000027B8
		public bool IsGameKeyDown(int gameKey)
		{
			GameKey gameKey2 = this._registeredGameKeys[gameKey];
			return this.IsGameKeyDown(gameKey2);
		}

		// Token: 0x0600014D RID: 333 RVA: 0x000045D9 File Offset: 0x000027D9
		private bool IsGameKeyDownImmediate(GameKey gameKey)
		{
			return gameKey.IsDownImmediate(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x0600014E RID: 334 RVA: 0x00004604 File Offset: 0x00002804
		public bool IsGameKeyDownImmediate(int gameKey)
		{
			GameKey gameKey2 = this._registeredGameKeys[gameKey];
			return this.IsGameKeyDownImmediate(gameKey2);
		}

		// Token: 0x0600014F RID: 335 RVA: 0x00004625 File Offset: 0x00002825
		private bool IsHotKeyPressed(HotKey hotKey)
		{
			return hotKey.IsPressed(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x06000150 RID: 336 RVA: 0x00004650 File Offset: 0x00002850
		public bool IsHotKeyPressed(string hotKey)
		{
			HotKey hotKey2;
			return this._registeredHotKeys.TryGetValue(hotKey, out hotKey2) && this.IsHotKeyPressed(hotKey2);
		}

		// Token: 0x06000151 RID: 337 RVA: 0x00004676 File Offset: 0x00002876
		private bool IsGameKeyPressed(GameKey gameKey)
		{
			return gameKey.IsPressed(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x06000152 RID: 338 RVA: 0x000046A4 File Offset: 0x000028A4
		public bool IsGameKeyPressed(int gameKey)
		{
			GameKey gameKey2 = this._registeredGameKeys[gameKey];
			return this.IsGameKeyPressed(gameKey2);
		}

		// Token: 0x06000153 RID: 339 RVA: 0x000046C8 File Offset: 0x000028C8
		private bool IsHotKeyReleased(HotKey hotKey)
		{
			for (int i = 0; i < hotKey.Keys.Count; i++)
			{
				if (this._downInputKeys.Contains(hotKey.Keys[i]))
				{
					return hotKey.IsReleased(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
				}
			}
			return false;
		}

		// Token: 0x06000154 RID: 340 RVA: 0x00004730 File Offset: 0x00002930
		public bool IsHotKeyReleased(string hotKey)
		{
			HotKey hotKey2;
			return this._registeredHotKeys.TryGetValue(hotKey, out hotKey2) && this.IsHotKeyReleased(hotKey2);
		}

		// Token: 0x06000155 RID: 341 RVA: 0x00004758 File Offset: 0x00002958
		private bool IsGameKeyReleased(GameKey gameKey)
		{
			return (this._downInputKeys.Contains(gameKey.KeyboardKey) || this._downInputKeys.Contains(gameKey.ControllerKey)) && gameKey.IsReleased(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x06000156 RID: 342 RVA: 0x000047B8 File Offset: 0x000029B8
		public bool IsGameKeyReleased(int gameKey)
		{
			GameKey gameKey2 = this._registeredGameKeys[gameKey];
			return this.IsGameKeyReleased(gameKey2);
		}

		// Token: 0x06000157 RID: 343 RVA: 0x000047D9 File Offset: 0x000029D9
		private float GetGameKeyState(GameKey gameKey)
		{
			return gameKey.GetKeyState(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00004804 File Offset: 0x00002A04
		public float GetGameKeyState(int gameKey)
		{
			GameKey gameKey2 = this._registeredGameKeys[gameKey];
			return this.GetGameKeyState(gameKey2);
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00004825 File Offset: 0x00002A25
		private bool IsHotKeyDoublePressed(HotKey hotKey)
		{
			return hotKey.IsDoublePressed(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00004850 File Offset: 0x00002A50
		public bool IsHotKeyDoublePressed(string hotKey)
		{
			HotKey hotKey2;
			return this._registeredHotKeys.TryGetValue(hotKey, out hotKey2) && this.IsHotKeyDoublePressed(hotKey2);
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00004876 File Offset: 0x00002A76
		public float GetGameKeyAxis(GameAxisKey gameKey)
		{
			return gameKey.GetAxisState(this.IsKeysAllowed, this.IsMouseButtonAllowed && this.MouseOnMe, this.IsMouseWheelAllowed, this.IsControllerAllowed);
		}

		// Token: 0x0600015C RID: 348 RVA: 0x000048A4 File Offset: 0x00002AA4
		public float GetGameKeyAxis(string gameKey)
		{
			GameAxisKey gameKey2;
			if (this._registeredGameAxisKeys.TryGetValue(gameKey, out gameKey2))
			{
				return this.GetGameKeyAxis(gameKey2);
			}
			return 0f;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x000048D0 File Offset: 0x00002AD0
		internal bool CanUse(InputKey key)
		{
			InputKey[] clickKeys = Input.GetClickKeys();
			for (int i = 0; i < clickKeys.Length; i++)
			{
				if (clickKeys[i] == key)
				{
					return this.IsMouseButtonAllowed || this.IsControllerAllowed;
				}
			}
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
				return this.IsKeysAllowed;
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
			case InputKey.ControllerLTrigger:
			case InputKey.ControllerRTrigger:
				return this.IsControllerAllowed;
			case InputKey.LeftMouseButton:
			case InputKey.RightMouseButton:
			case InputKey.MiddleMouseButton:
			case InputKey.X1MouseButton:
			case InputKey.X2MouseButton:
				return this.IsMouseButtonAllowed;
			case InputKey.MouseScrollUp:
			case InputKey.MouseScrollDown:
				return this.IsMouseWheelAllowed;
			}
			return false;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00004D2B File Offset: 0x00002F2B
		public Vec2 GetKeyState(InputKey key)
		{
			if (!this.CanUse(key))
			{
				return new Vec2(0f, 0f);
			}
			return Input.GetKeyState(key);
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00004D4C File Offset: 0x00002F4C
		protected bool IsMouseButton(InputKey key)
		{
			return key == InputKey.LeftMouseButton || key == InputKey.RightMouseButton || key == InputKey.MiddleMouseButton;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00004D68 File Offset: 0x00002F68
		public bool IsKeyDown(InputKey key)
		{
			if (this.IsMouseButton(key))
			{
				if (!this.MouseOnMe)
				{
					return false;
				}
			}
			else if (!this.CanUse(key))
			{
				return false;
			}
			return Input.IsKeyDown(key);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00004D8E File Offset: 0x00002F8E
		public bool IsKeyPressed(InputKey key)
		{
			return this.CanUse(key) && Input.IsKeyPressed(key);
		}

		// Token: 0x06000162 RID: 354 RVA: 0x00004DA1 File Offset: 0x00002FA1
		public bool IsKeyReleased(InputKey key)
		{
			if (this.IsMouseButton(key))
			{
				if (!this.MouseOnMe)
				{
					return false;
				}
			}
			else if (!this.CanUse(key))
			{
				return false;
			}
			return Input.IsKeyReleased(key);
		}

		// Token: 0x06000163 RID: 355 RVA: 0x00004DC7 File Offset: 0x00002FC7
		public float GetMouseMoveX()
		{
			return Input.GetMouseMoveX();
		}

		// Token: 0x06000164 RID: 356 RVA: 0x00004DCE File Offset: 0x00002FCE
		public float GetMouseMoveY()
		{
			return Input.GetMouseMoveY();
		}

		// Token: 0x06000165 RID: 357 RVA: 0x00004DD5 File Offset: 0x00002FD5
		public float GetNormalizedMouseMoveX()
		{
			return Input.GetNormalizedMouseMoveX();
		}

		// Token: 0x06000166 RID: 358 RVA: 0x00004DDC File Offset: 0x00002FDC
		public float GetNormalizedMouseMoveY()
		{
			return Input.GetNormalizedMouseMoveY();
		}

		// Token: 0x06000167 RID: 359 RVA: 0x00004DE3 File Offset: 0x00002FE3
		public Vec2 GetControllerRightStickState()
		{
			return Input.GetKeyState(InputKey.ControllerRStick);
		}

		// Token: 0x06000168 RID: 360 RVA: 0x00004DEF File Offset: 0x00002FEF
		public Vec2 GetControllerLeftStickState()
		{
			return Input.GetKeyState(InputKey.ControllerLStick);
		}

		// Token: 0x06000169 RID: 361 RVA: 0x00004DFB File Offset: 0x00002FFB
		public bool GetIsMouseActive()
		{
			return Input.IsMouseActive;
		}

		// Token: 0x0600016A RID: 362 RVA: 0x00004E02 File Offset: 0x00003002
		public bool GetIsMouseDown()
		{
			return Input.IsKeyDown(InputKey.LeftMouseButton) || Input.IsKeyDown(InputKey.RightMouseButton);
		}

		// Token: 0x0600016B RID: 363 RVA: 0x00004E1C File Offset: 0x0000301C
		public Vec2 GetMousePositionPixel()
		{
			return Input.MousePositionPixel;
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00004E23 File Offset: 0x00003023
		public float GetDeltaMouseScroll()
		{
			if (!this.IsMouseWheelAllowed)
			{
				return 0f;
			}
			return Input.DeltaMouseScroll;
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00004E38 File Offset: 0x00003038
		public bool GetIsControllerConnected()
		{
			return Input.IsControllerConnected;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00004E3F File Offset: 0x0000303F
		public Vec2 GetMousePositionRanged()
		{
			return Input.MousePositionRanged;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00004E46 File Offset: 0x00003046
		public float GetMouseSensitivity()
		{
			return Input.MouseSensitivity;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x00004E4D File Offset: 0x0000304D
		public bool IsControlDown()
		{
			return this.IsKeysAllowed && (Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl));
		}

		// Token: 0x06000171 RID: 369 RVA: 0x00004E6E File Offset: 0x0000306E
		public bool IsShiftDown()
		{
			return this.IsKeysAllowed && (Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift));
		}

		// Token: 0x06000172 RID: 370 RVA: 0x00004E8C File Offset: 0x0000308C
		public bool IsAltDown()
		{
			return this.IsKeysAllowed && (Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt));
		}

		// Token: 0x06000173 RID: 371 RVA: 0x00004EAD File Offset: 0x000030AD
		public InputKey[] GetClickKeys()
		{
			return Input.GetClickKeys();
		}

		// Token: 0x04000033 RID: 51
		private List<GameKey> _registeredGameKeys;

		// Token: 0x04000034 RID: 52
		private Dictionary<string, HotKey> _registeredHotKeys;

		// Token: 0x04000035 RID: 53
		private Dictionary<string, GameAxisKey> _registeredGameAxisKeys;

		// Token: 0x04000036 RID: 54
		private List<Key> _downInputKeys;

		// Token: 0x04000037 RID: 55
		private List<Key> _allKeysListMemoryCache = new List<Key>();

		// Token: 0x0400003C RID: 60
		private readonly List<GameKeyContext> _categories;
	}
}
