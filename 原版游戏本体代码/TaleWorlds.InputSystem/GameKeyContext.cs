using System;
using System.Collections.Generic;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000006 RID: 6
	public abstract class GameKeyContext
	{
		// Token: 0x17000010 RID: 16
		// (get) Token: 0x06000074 RID: 116 RVA: 0x0000296A File Offset: 0x00000B6A
		// (set) Token: 0x06000075 RID: 117 RVA: 0x00002972 File Offset: 0x00000B72
		public string GameKeyCategoryId { get; private set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000076 RID: 118 RVA: 0x0000297B File Offset: 0x00000B7B
		// (set) Token: 0x06000077 RID: 119 RVA: 0x00002983 File Offset: 0x00000B83
		public GameKeyContext.GameKeyContextType Type { get; private set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000078 RID: 120 RVA: 0x0000298C File Offset: 0x00000B8C
		public MBReadOnlyList<GameKey> RegisteredGameKeys
		{
			get
			{
				return this._registeredGameKeys;
			}
		}

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000079 RID: 121 RVA: 0x00002994 File Offset: 0x00000B94
		public Dictionary<string, HotKey>.ValueCollection RegisteredHotKeys
		{
			get
			{
				return this._registeredHotKeys.Values;
			}
		}

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600007A RID: 122 RVA: 0x000029A1 File Offset: 0x00000BA1
		public Dictionary<string, GameAxisKey>.ValueCollection RegisteredGameAxisKeys
		{
			get
			{
				return this._registeredAxisKeys.Values;
			}
		}

		// Token: 0x0600007B RID: 123 RVA: 0x000029B0 File Offset: 0x00000BB0
		protected GameKeyContext(string id, int gameKeysCount, GameKeyContext.GameKeyContextType type = GameKeyContext.GameKeyContextType.Default)
		{
			this.GameKeyCategoryId = id;
			this.Type = type;
			this._registeredHotKeys = new Dictionary<string, HotKey>();
			this._registeredAxisKeys = new Dictionary<string, GameAxisKey>();
			this._registeredGameKeys = new MBList<GameKey>(gameKeysCount);
			for (int i = 0; i < gameKeysCount; i++)
			{
				this._registeredGameKeys.Add(null);
			}
		}

		// Token: 0x0600007C RID: 124 RVA: 0x00002A0C File Offset: 0x00000C0C
		protected internal void RegisterHotKey(HotKey gameKey, bool addIfMissing = true)
		{
			if (GameKeyContext._isRDownSwappedWithRRight)
			{
				for (int i = 0; i < gameKey.Keys.Count; i++)
				{
					Key key = gameKey.Keys[i];
					if (key != null && key.InputKey == InputKey.ControllerRDown)
					{
						key.ChangeKey(InputKey.ControllerRRight);
					}
					else if (key != null && key.InputKey == InputKey.ControllerRRight)
					{
						key.ChangeKey(InputKey.ControllerRDown);
					}
				}
			}
			if (this._registeredHotKeys.ContainsKey(gameKey.Id))
			{
				this._registeredHotKeys[gameKey.Id] = gameKey;
				return;
			}
			if (addIfMissing)
			{
				this._registeredHotKeys.Add(gameKey.Id, gameKey);
			}
		}

		// Token: 0x0600007D RID: 125 RVA: 0x00002AB8 File Offset: 0x00000CB8
		protected internal void RegisterGameKey(GameKey gameKey, bool addIfMissing = true)
		{
			if (GameKeyContext._isRDownSwappedWithRRight)
			{
				Key controllerKey = gameKey.ControllerKey;
				if (controllerKey != null && controllerKey.InputKey == InputKey.ControllerRDown)
				{
					controllerKey.ChangeKey(InputKey.ControllerRRight);
				}
				else if (controllerKey != null && controllerKey.InputKey == InputKey.ControllerRRight)
				{
					controllerKey.ChangeKey(InputKey.ControllerRDown);
				}
			}
			this._registeredGameKeys[gameKey.Id] = gameKey;
		}

		// Token: 0x0600007E RID: 126 RVA: 0x00002B1D File Offset: 0x00000D1D
		protected internal void RegisterGameAxisKey(GameAxisKey gameKey, bool addIfMissing = true)
		{
			if (this._registeredAxisKeys.ContainsKey(gameKey.Id))
			{
				this._registeredAxisKeys[gameKey.Id] = gameKey;
				return;
			}
			if (addIfMissing)
			{
				this._registeredAxisKeys.Add(gameKey.Id, gameKey);
			}
		}

		// Token: 0x0600007F RID: 127 RVA: 0x00002B5A File Offset: 0x00000D5A
		internal static void SetIsRDownSwappedWithRRight(bool value)
		{
			GameKeyContext._isRDownSwappedWithRRight = value;
		}

		// Token: 0x06000080 RID: 128 RVA: 0x00002B64 File Offset: 0x00000D64
		public HotKey GetHotKey(string hotKeyId)
		{
			HotKey result = null;
			this._registeredHotKeys.TryGetValue(hotKeyId, out result);
			return result;
		}

		// Token: 0x06000081 RID: 129 RVA: 0x00002B84 File Offset: 0x00000D84
		public GameKey GetGameKey(int gameKeyId)
		{
			for (int i = 0; i < this._registeredGameKeys.Count; i++)
			{
				GameKey gameKey = this._registeredGameKeys[i];
				if (gameKey != null && gameKey.Id == gameKeyId)
				{
					return gameKey;
				}
			}
			Debug.FailedAssert(string.Format("Couldn't find {0} in {1}", gameKeyId, this.GameKeyCategoryId), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetGameKey", 125);
			return null;
		}

		// Token: 0x06000082 RID: 130 RVA: 0x00002BEC File Offset: 0x00000DEC
		internal GameKey GetGameKey(string gameKeyId)
		{
			for (int i = 0; i < this._registeredGameKeys.Count; i++)
			{
				GameKey gameKey = this._registeredGameKeys[i];
				if (gameKey != null && gameKey.StringId == gameKeyId)
				{
					return gameKey;
				}
			}
			Debug.FailedAssert("Couldn't find " + gameKeyId + " in " + this.GameKeyCategoryId, "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetGameKey", 140);
			return null;
		}

		// Token: 0x06000083 RID: 131 RVA: 0x00002C5C File Offset: 0x00000E5C
		internal GameAxisKey GetGameAxisKey(string axisKeyId)
		{
			GameAxisKey result;
			this._registeredAxisKeys.TryGetValue(axisKeyId, out result);
			return result;
		}

		// Token: 0x06000084 RID: 132 RVA: 0x00002C7C File Offset: 0x00000E7C
		public string GetHotKeyId(string hotKeyId)
		{
			HotKey hotKey;
			if (this._registeredHotKeys.TryGetValue(hotKeyId, out hotKey))
			{
				return hotKey.ToString();
			}
			GameAxisKey gameAxisKey;
			if (this._registeredAxisKeys.TryGetValue(hotKeyId, out gameAxisKey))
			{
				return gameAxisKey.ToString();
			}
			Debug.FailedAssert("HotKey with id: " + hotKeyId + " is not registered.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetHotKeyId", 163);
			return "";
		}

		// Token: 0x06000085 RID: 133 RVA: 0x00002CE0 File Offset: 0x00000EE0
		public string GetHotKeyId(int gameKeyId)
		{
			GameKey gameKey = this._registeredGameKeys[gameKeyId];
			if (gameKey != null)
			{
				return gameKey.ToString();
			}
			Debug.FailedAssert("GameKey with id: " + gameKeyId + " is not registered.", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.InputSystem\\GameKeyContext.cs", "GetHotKeyId", 175);
			return "";
		}

		// Token: 0x04000012 RID: 18
		private readonly Dictionary<string, HotKey> _registeredHotKeys;

		// Token: 0x04000013 RID: 19
		private readonly MBList<GameKey> _registeredGameKeys;

		// Token: 0x04000014 RID: 20
		private readonly Dictionary<string, GameAxisKey> _registeredAxisKeys;

		// Token: 0x04000015 RID: 21
		private static bool _isRDownSwappedWithRRight = true;

		// Token: 0x02000012 RID: 18
		public enum GameKeyContextType
		{
			// Token: 0x04000154 RID: 340
			Default,
			// Token: 0x04000155 RID: 341
			AuxiliaryNotSerialized,
			// Token: 0x04000156 RID: 342
			AuxiliarySerialized,
			// Token: 0x04000157 RID: 343
			AuxiliarySerializedAndShownInOptions
		}
	}
}
