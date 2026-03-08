using System;

namespace System
{
	// Token: 0x020000C7 RID: 199
	[Serializable]
	public struct ConsoleKeyInfo
	{
		// Token: 0x06000B9C RID: 2972 RVA: 0x00024FC4 File Offset: 0x000231C4
		public ConsoleKeyInfo(char keyChar, ConsoleKey key, bool shift, bool alt, bool control)
		{
			if (key < (ConsoleKey)0 || key > (ConsoleKey)255)
			{
				throw new ArgumentOutOfRangeException("key", Environment.GetResourceString("ArgumentOutOfRange_ConsoleKey"));
			}
			this._keyChar = keyChar;
			this._key = key;
			this._mods = (ConsoleModifiers)0;
			if (shift)
			{
				this._mods |= ConsoleModifiers.Shift;
			}
			if (alt)
			{
				this._mods |= ConsoleModifiers.Alt;
			}
			if (control)
			{
				this._mods |= ConsoleModifiers.Control;
			}
		}

		// Token: 0x1700017F RID: 383
		// (get) Token: 0x06000B9D RID: 2973 RVA: 0x0002503C File Offset: 0x0002323C
		public char KeyChar
		{
			get
			{
				return this._keyChar;
			}
		}

		// Token: 0x17000180 RID: 384
		// (get) Token: 0x06000B9E RID: 2974 RVA: 0x00025044 File Offset: 0x00023244
		public ConsoleKey Key
		{
			get
			{
				return this._key;
			}
		}

		// Token: 0x17000181 RID: 385
		// (get) Token: 0x06000B9F RID: 2975 RVA: 0x0002504C File Offset: 0x0002324C
		public ConsoleModifiers Modifiers
		{
			get
			{
				return this._mods;
			}
		}

		// Token: 0x06000BA0 RID: 2976 RVA: 0x00025054 File Offset: 0x00023254
		public override bool Equals(object value)
		{
			return value is ConsoleKeyInfo && this.Equals((ConsoleKeyInfo)value);
		}

		// Token: 0x06000BA1 RID: 2977 RVA: 0x0002506C File Offset: 0x0002326C
		public bool Equals(ConsoleKeyInfo obj)
		{
			return obj._keyChar == this._keyChar && obj._key == this._key && obj._mods == this._mods;
		}

		// Token: 0x06000BA2 RID: 2978 RVA: 0x0002509A File Offset: 0x0002329A
		public static bool operator ==(ConsoleKeyInfo a, ConsoleKeyInfo b)
		{
			return a.Equals(b);
		}

		// Token: 0x06000BA3 RID: 2979 RVA: 0x000250A4 File Offset: 0x000232A4
		public static bool operator !=(ConsoleKeyInfo a, ConsoleKeyInfo b)
		{
			return !(a == b);
		}

		// Token: 0x06000BA4 RID: 2980 RVA: 0x000250B0 File Offset: 0x000232B0
		public override int GetHashCode()
		{
			return (int)((ConsoleModifiers)this._keyChar | this._mods);
		}

		// Token: 0x04000527 RID: 1319
		private char _keyChar;

		// Token: 0x04000528 RID: 1320
		private ConsoleKey _key;

		// Token: 0x04000529 RID: 1321
		private ConsoleModifiers _mods;
	}
}
