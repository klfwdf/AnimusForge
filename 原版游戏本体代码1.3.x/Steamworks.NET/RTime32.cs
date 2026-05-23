using System;

namespace Steamworks
{
	// Token: 0x020001A9 RID: 425
	[Serializable]
	public struct RTime32 : IEquatable<RTime32>, IComparable<RTime32>
	{
		// Token: 0x06000A78 RID: 2680 RVA: 0x0000DE6D File Offset: 0x0000C06D
		public RTime32(uint value)
		{
			this.m_RTime32 = value;
		}

		// Token: 0x06000A79 RID: 2681 RVA: 0x0000DE76 File Offset: 0x0000C076
		public override string ToString()
		{
			return this.m_RTime32.ToString();
		}

		// Token: 0x06000A7A RID: 2682 RVA: 0x0000DE83 File Offset: 0x0000C083
		public override bool Equals(object other)
		{
			return other is RTime32 && this == (RTime32)other;
		}

		// Token: 0x06000A7B RID: 2683 RVA: 0x0000DEA0 File Offset: 0x0000C0A0
		public override int GetHashCode()
		{
			return this.m_RTime32.GetHashCode();
		}

		// Token: 0x06000A7C RID: 2684 RVA: 0x0000DEAD File Offset: 0x0000C0AD
		public static bool operator ==(RTime32 x, RTime32 y)
		{
			return x.m_RTime32 == y.m_RTime32;
		}

		// Token: 0x06000A7D RID: 2685 RVA: 0x0000DEBD File Offset: 0x0000C0BD
		public static bool operator !=(RTime32 x, RTime32 y)
		{
			return !(x == y);
		}

		// Token: 0x06000A7E RID: 2686 RVA: 0x0000DEC9 File Offset: 0x0000C0C9
		public static explicit operator RTime32(uint value)
		{
			return new RTime32(value);
		}

		// Token: 0x06000A7F RID: 2687 RVA: 0x0000DED1 File Offset: 0x0000C0D1
		public static explicit operator uint(RTime32 that)
		{
			return that.m_RTime32;
		}

		// Token: 0x06000A80 RID: 2688 RVA: 0x0000DEAD File Offset: 0x0000C0AD
		public bool Equals(RTime32 other)
		{
			return this.m_RTime32 == other.m_RTime32;
		}

		// Token: 0x06000A81 RID: 2689 RVA: 0x0000DED9 File Offset: 0x0000C0D9
		public int CompareTo(RTime32 other)
		{
			return this.m_RTime32.CompareTo(other.m_RTime32);
		}

		// Token: 0x04000A60 RID: 2656
		public uint m_RTime32;
	}
}
