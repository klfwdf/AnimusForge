using System;

namespace Steamworks
{
	// Token: 0x020001A6 RID: 422
	[Serializable]
	public struct AppId_t : IEquatable<AppId_t>, IComparable<AppId_t>
	{
		// Token: 0x06000A57 RID: 2647 RVA: 0x0000DCC8 File Offset: 0x0000BEC8
		public AppId_t(uint value)
		{
			this.m_AppId = value;
		}

		// Token: 0x06000A58 RID: 2648 RVA: 0x0000DCD1 File Offset: 0x0000BED1
		public override string ToString()
		{
			return this.m_AppId.ToString();
		}

		// Token: 0x06000A59 RID: 2649 RVA: 0x0000DCDE File Offset: 0x0000BEDE
		public override bool Equals(object other)
		{
			return other is AppId_t && this == (AppId_t)other;
		}

		// Token: 0x06000A5A RID: 2650 RVA: 0x0000DCFB File Offset: 0x0000BEFB
		public override int GetHashCode()
		{
			return this.m_AppId.GetHashCode();
		}

		// Token: 0x06000A5B RID: 2651 RVA: 0x0000DD08 File Offset: 0x0000BF08
		public static bool operator ==(AppId_t x, AppId_t y)
		{
			return x.m_AppId == y.m_AppId;
		}

		// Token: 0x06000A5C RID: 2652 RVA: 0x0000DD18 File Offset: 0x0000BF18
		public static bool operator !=(AppId_t x, AppId_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A5D RID: 2653 RVA: 0x0000DD24 File Offset: 0x0000BF24
		public static explicit operator AppId_t(uint value)
		{
			return new AppId_t(value);
		}

		// Token: 0x06000A5E RID: 2654 RVA: 0x0000DD2C File Offset: 0x0000BF2C
		public static explicit operator uint(AppId_t that)
		{
			return that.m_AppId;
		}

		// Token: 0x06000A5F RID: 2655 RVA: 0x0000DD08 File Offset: 0x0000BF08
		public bool Equals(AppId_t other)
		{
			return this.m_AppId == other.m_AppId;
		}

		// Token: 0x06000A60 RID: 2656 RVA: 0x0000DD34 File Offset: 0x0000BF34
		public int CompareTo(AppId_t other)
		{
			return this.m_AppId.CompareTo(other.m_AppId);
		}

		// Token: 0x04000A5A RID: 2650
		public static readonly AppId_t Invalid = new AppId_t(0U);

		// Token: 0x04000A5B RID: 2651
		public uint m_AppId;
	}
}
