using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;

namespace TaleWorlds.InputSystem
{
	// Token: 0x02000007 RID: 7
	public class HotKey
	{
		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000087 RID: 135 RVA: 0x00002D3C File Offset: 0x00000F3C
		private bool _isDoublePressActive
		{
			get
			{
				int num = Environment.TickCount - this._doublePressTime;
				return num < 500 && num >= 0;
			}
		}

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x06000088 RID: 136 RVA: 0x00002D67 File Offset: 0x00000F67
		// (set) Token: 0x06000089 RID: 137 RVA: 0x00002D6F File Offset: 0x00000F6F
		public List<Key> Keys { get; internal set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600008A RID: 138 RVA: 0x00002D78 File Offset: 0x00000F78
		// (set) Token: 0x0600008B RID: 139 RVA: 0x00002D80 File Offset: 0x00000F80
		public List<Key> DefaultKeys { get; private set; }

		// Token: 0x0600008C RID: 140 RVA: 0x00002D8C File Offset: 0x00000F8C
		public HotKey(string id, string groupId, List<Key> keys, HotKey.Modifiers modifiers = HotKey.Modifiers.None, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
		{
			this.Id = id;
			this.GroupId = groupId;
			this.Keys = keys;
			this.DefaultKeys = new List<Key>();
			for (int i = 0; i < this.Keys.Count; i++)
			{
				this.DefaultKeys.Add(new Key(this.Keys[i].InputKey));
			}
			this._modifiers = modifiers;
			this._negativeModifiers = negativeModifiers;
		}

		// Token: 0x0600008D RID: 141 RVA: 0x00002E08 File Offset: 0x00001008
		public HotKey(string id, string groupId, InputKey inputKey, HotKey.Modifiers modifiers = HotKey.Modifiers.None, HotKey.Modifiers negativeModifiers = HotKey.Modifiers.None)
		{
			this.Id = id;
			this.GroupId = groupId;
			this.Keys = new List<Key>
			{
				new Key(inputKey)
			};
			this.DefaultKeys = new List<Key>
			{
				new Key(inputKey)
			};
			this._modifiers = modifiers;
			this._negativeModifiers = negativeModifiers;
		}

		// Token: 0x0600008E RID: 142 RVA: 0x00002E67 File Offset: 0x00001067
		private bool IsKeyAllowed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return (isKeysAllowed || !key.IsKeyboardInput) && (isMouseButtonAllowed || !key.IsMouseButtonInput) && (isMouseWheelAllowed || !key.IsMouseWheelInput) && (isControllerAllowed || !key.IsControllerInput);
		}

		// Token: 0x0600008F RID: 143 RVA: 0x00002E9C File Offset: 0x0000109C
		private bool CheckModifiers()
		{
			bool flag = Input.IsKeyDown(InputKey.LeftControl) || Input.IsKeyDown(InputKey.RightControl);
			bool flag2 = Input.IsKeyDown(InputKey.LeftAlt) || Input.IsKeyDown(InputKey.RightAlt);
			bool flag3 = Input.IsKeyDown(InputKey.LeftShift) || Input.IsKeyDown(InputKey.RightShift);
			bool flag4 = true;
			bool flag5 = true;
			bool flag6 = true;
			if (this._modifiers.HasAnyFlag(HotKey.Modifiers.Control))
			{
				flag4 = flag;
			}
			if (this._modifiers.HasAnyFlag(HotKey.Modifiers.Alt))
			{
				flag5 = flag2;
			}
			if (this._modifiers.HasAnyFlag(HotKey.Modifiers.Shift))
			{
				flag6 = flag3;
			}
			if (this._negativeModifiers.HasAnyFlag(HotKey.Modifiers.Control))
			{
				flag4 = !flag;
			}
			if (this._negativeModifiers.HasAnyFlag(HotKey.Modifiers.Alt))
			{
				flag5 = !flag2;
			}
			if (this._negativeModifiers.HasAnyFlag(HotKey.Modifiers.Shift))
			{
				flag6 = !flag3;
			}
			return flag4 && flag5 && flag6;
		}

		// Token: 0x06000090 RID: 144 RVA: 0x00002F67 File Offset: 0x00001167
		private bool IsDown(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return this.IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed) && (this._modifiers == HotKey.Modifiers.None || this.CheckModifiers()) && key.IsDown();
		}

		// Token: 0x06000091 RID: 145 RVA: 0x00002F94 File Offset: 0x00001194
		internal bool IsDown(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			foreach (Key key in this.Keys)
			{
				if (this.IsDown(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000092 RID: 146 RVA: 0x00002FF8 File Offset: 0x000011F8
		private bool IsDownImmediate(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return this.IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed) && (this._modifiers == HotKey.Modifiers.None || this.CheckModifiers()) && key.IsDownImmediate();
		}

		// Token: 0x06000093 RID: 147 RVA: 0x00003024 File Offset: 0x00001224
		internal bool IsDownImmediate(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			foreach (Key key in this.Keys)
			{
				if (this.IsDownImmediate(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000094 RID: 148 RVA: 0x00003088 File Offset: 0x00001288
		private bool IsDoublePressed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			if (!this.IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
			{
				return false;
			}
			if (this._modifiers != HotKey.Modifiers.None && !this.CheckModifiers())
			{
				return false;
			}
			if (key.IsPressed())
			{
				if (this._isDoublePressActive)
				{
					this._doublePressTime = 0;
					return true;
				}
				this._doublePressTime = Environment.TickCount;
			}
			return false;
		}

		// Token: 0x06000095 RID: 149 RVA: 0x000030E0 File Offset: 0x000012E0
		internal bool IsDoublePressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			foreach (Key key in this.Keys)
			{
				if (this.IsDoublePressed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000096 RID: 150 RVA: 0x00003144 File Offset: 0x00001344
		private bool IsPressed(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return this.IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed) && (this._modifiers == HotKey.Modifiers.None || this.CheckModifiers()) && key.IsPressed();
		}

		// Token: 0x06000097 RID: 151 RVA: 0x00003170 File Offset: 0x00001370
		internal bool IsPressed(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			foreach (Key key in this.Keys)
			{
				if (this.IsPressed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x000031D4 File Offset: 0x000013D4
		private bool IsReleased(Key key, bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			return this.IsKeyAllowed(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed) && (this._modifiers == HotKey.Modifiers.None || this.CheckModifiers()) && key.IsReleased();
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00003200 File Offset: 0x00001400
		internal bool IsReleased(bool isKeysAllowed, bool isMouseButtonAllowed, bool isMouseWheelAllowed, bool isControllerAllowed)
		{
			foreach (Key key in this.Keys)
			{
				if (this.IsReleased(key, isKeysAllowed, isMouseButtonAllowed, isMouseWheelAllowed, isControllerAllowed))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x0600009A RID: 154 RVA: 0x00003264 File Offset: 0x00001464
		public bool HasModifier(HotKey.Modifiers modifier)
		{
			return this._modifiers.HasAnyFlag(modifier);
		}

		// Token: 0x0600009B RID: 155 RVA: 0x00003272 File Offset: 0x00001472
		public bool HasSameModifiers(HotKey other)
		{
			return this._modifiers == other._modifiers;
		}

		// Token: 0x0600009C RID: 156 RVA: 0x00003284 File Offset: 0x00001484
		public override string ToString()
		{
			string result = "";
			bool flag = Input.IsControllerConnected && !Input.IsMouseActive;
			for (int i = 0; i < this.Keys.Count; i++)
			{
				if ((!flag && !this.Keys[i].IsControllerInput) || (flag && this.Keys[i].IsControllerInput))
				{
					return this.Keys[i].ToString();
				}
			}
			return result;
		}

		// Token: 0x0600009D RID: 157 RVA: 0x00003300 File Offset: 0x00001500
		public override bool Equals(object obj)
		{
			HotKey hotKey = obj as HotKey;
			return hotKey != null && hotKey.Id.Equals(this.Id) && hotKey.GroupId.Equals(this.GroupId) && hotKey.Keys.SequenceEqual(this.Keys);
		}

		// Token: 0x0600009E RID: 158 RVA: 0x00003350 File Offset: 0x00001550
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}

		// Token: 0x04000016 RID: 22
		private const int DOUBLE_PRESS_TIME = 500;

		// Token: 0x04000017 RID: 23
		private int _doublePressTime;

		// Token: 0x04000018 RID: 24
		public string Id;

		// Token: 0x04000019 RID: 25
		public string GroupId;

		// Token: 0x0400001C RID: 28
		private HotKey.Modifiers _modifiers;

		// Token: 0x0400001D RID: 29
		private HotKey.Modifiers _negativeModifiers;

		// Token: 0x02000013 RID: 19
		[Flags]
		public enum Modifiers
		{
			// Token: 0x04000159 RID: 345
			None = 0,
			// Token: 0x0400015A RID: 346
			Shift = 1,
			// Token: 0x0400015B RID: 347
			Alt = 2,
			// Token: 0x0400015C RID: 348
			Control = 4
		}
	}
}
