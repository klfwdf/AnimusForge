using System;

namespace Steamworks
{
	// Token: 0x020001AA RID: 426
	[Serializable]
	public struct SteamAPICall_t : IEquatable<SteamAPICall_t>, IComparable<SteamAPICall_t>
	{
		// Token: 0x06000A82 RID: 2690 RVA: 0x0000DEEC File Offset: 0x0000C0EC
		public SteamAPICall_t(ulong value)
		{
			this.m_SteamAPICall = value;
		}

		// Token: 0x06000A83 RID: 2691 RVA: 0x0000DEF5 File Offset: 0x0000C0F5
		public override string ToString()
		{
			return this.m_SteamAPICall.ToString();
		}

		// Token: 0x06000A84 RID: 2692 RVA: 0x0000DF02 File Offset: 0x0000C102
		public override bool Equals(object other)
		{
			return other is SteamAPICall_t && this == (SteamAPICall_t)other;
		}

		// Token: 0x06000A85 RID: 2693 RVA: 0x0000DF1F File Offset: 0x0000C11F
		public override int GetHashCode()
		{
			return this.m_SteamAPICall.GetHashCode();
		}

		// Token: 0x06000A86 RID: 2694 RVA: 0x0000DF2C File Offset: 0x0000C12C
		public static bool operator ==(SteamAPICall_t x, SteamAPICall_t y)
		{
			return x.m_SteamAPICall == y.m_SteamAPICall;
		}

		// Token: 0x06000A87 RID: 2695 RVA: 0x0000DF3C File Offset: 0x0000C13C
		public static bool operator !=(SteamAPICall_t x, SteamAPICall_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A88 RID: 2696 RVA: 0x0000DF48 File Offset: 0x0000C148
		public static explicit operator SteamAPICall_t(ulong value)
		{
			return new SteamAPICall_t(value);
		}

		// Token: 0x06000A89 RID: 2697 RVA: 0x0000DF50 File Offset: 0x0000C150
		public static explicit operator ulong(SteamAPICall_t that)
		{
			return that.m_SteamAPICall;
		}

		// Token: 0x06000A8A RID: 2698 RVA: 0x0000DF2C File Offset: 0x0000C12C
		public bool Equals(SteamAPICall_t other)
		{
			return this.m_SteamAPICall == other.m_SteamAPICall;
		}

		// Token: 0x06000A8B RID: 2699 RVA: 0x0000DF58 File Offset: 0x0000C158
		public int CompareTo(SteamAPICall_t other)
		{
			return this.m_SteamAPICall.CompareTo(other.m_SteamAPICall);
		}

		// Token: 0x04000A61 RID: 2657
		public static readonly SteamAPICall_t Invalid = new SteamAPICall_t(0UL);

		// Token: 0x04000A62 RID: 2658
		public ulong m_SteamAPICall;
	}
}
