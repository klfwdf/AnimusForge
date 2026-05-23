using System;

namespace Steamworks
{
	// Token: 0x02000180 RID: 384
	[Serializable]
	public struct FriendsGroupID_t : IEquatable<FriendsGroupID_t>, IComparable<FriendsGroupID_t>
	{
		// Token: 0x060008FE RID: 2302 RVA: 0x0000CB93 File Offset: 0x0000AD93
		public FriendsGroupID_t(short value)
		{
			this.m_FriendsGroupID = value;
		}

		// Token: 0x060008FF RID: 2303 RVA: 0x0000CB9C File Offset: 0x0000AD9C
		public override string ToString()
		{
			return this.m_FriendsGroupID.ToString();
		}

		// Token: 0x06000900 RID: 2304 RVA: 0x0000CBA9 File Offset: 0x0000ADA9
		public override bool Equals(object other)
		{
			return other is FriendsGroupID_t && this == (FriendsGroupID_t)other;
		}

		// Token: 0x06000901 RID: 2305 RVA: 0x0000CBC6 File Offset: 0x0000ADC6
		public override int GetHashCode()
		{
			return this.m_FriendsGroupID.GetHashCode();
		}

		// Token: 0x06000902 RID: 2306 RVA: 0x0000CBD3 File Offset: 0x0000ADD3
		public static bool operator ==(FriendsGroupID_t x, FriendsGroupID_t y)
		{
			return x.m_FriendsGroupID == y.m_FriendsGroupID;
		}

		// Token: 0x06000903 RID: 2307 RVA: 0x0000CBE3 File Offset: 0x0000ADE3
		public static bool operator !=(FriendsGroupID_t x, FriendsGroupID_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000904 RID: 2308 RVA: 0x0000CBEF File Offset: 0x0000ADEF
		public static explicit operator FriendsGroupID_t(short value)
		{
			return new FriendsGroupID_t(value);
		}

		// Token: 0x06000905 RID: 2309 RVA: 0x0000CBF7 File Offset: 0x0000ADF7
		public static explicit operator short(FriendsGroupID_t that)
		{
			return that.m_FriendsGroupID;
		}

		// Token: 0x06000906 RID: 2310 RVA: 0x0000CBD3 File Offset: 0x0000ADD3
		public bool Equals(FriendsGroupID_t other)
		{
			return this.m_FriendsGroupID == other.m_FriendsGroupID;
		}

		// Token: 0x06000907 RID: 2311 RVA: 0x0000CBFF File Offset: 0x0000ADFF
		public int CompareTo(FriendsGroupID_t other)
		{
			return this.m_FriendsGroupID.CompareTo(other.m_FriendsGroupID);
		}

		// Token: 0x04000A0F RID: 2575
		public static readonly FriendsGroupID_t Invalid = new FriendsGroupID_t(-1);

		// Token: 0x04000A10 RID: 2576
		public short m_FriendsGroupID;
	}
}
