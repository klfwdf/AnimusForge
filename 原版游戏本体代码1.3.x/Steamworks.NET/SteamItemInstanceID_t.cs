using System;

namespace Steamworks
{
	// Token: 0x0200018D RID: 397
	[Serializable]
	public struct SteamItemInstanceID_t : IEquatable<SteamItemInstanceID_t>, IComparable<SteamItemInstanceID_t>
	{
		// Token: 0x06000976 RID: 2422 RVA: 0x0000D157 File Offset: 0x0000B357
		public SteamItemInstanceID_t(ulong value)
		{
			this.m_SteamItemInstanceID = value;
		}

		// Token: 0x06000977 RID: 2423 RVA: 0x0000D160 File Offset: 0x0000B360
		public override string ToString()
		{
			return this.m_SteamItemInstanceID.ToString();
		}

		// Token: 0x06000978 RID: 2424 RVA: 0x0000D16D File Offset: 0x0000B36D
		public override bool Equals(object other)
		{
			return other is SteamItemInstanceID_t && this == (SteamItemInstanceID_t)other;
		}

		// Token: 0x06000979 RID: 2425 RVA: 0x0000D18A File Offset: 0x0000B38A
		public override int GetHashCode()
		{
			return this.m_SteamItemInstanceID.GetHashCode();
		}

		// Token: 0x0600097A RID: 2426 RVA: 0x0000D197 File Offset: 0x0000B397
		public static bool operator ==(SteamItemInstanceID_t x, SteamItemInstanceID_t y)
		{
			return x.m_SteamItemInstanceID == y.m_SteamItemInstanceID;
		}

		// Token: 0x0600097B RID: 2427 RVA: 0x0000D1A7 File Offset: 0x0000B3A7
		public static bool operator !=(SteamItemInstanceID_t x, SteamItemInstanceID_t y)
		{
			return !(x == y);
		}

		// Token: 0x0600097C RID: 2428 RVA: 0x0000D1B3 File Offset: 0x0000B3B3
		public static explicit operator SteamItemInstanceID_t(ulong value)
		{
			return new SteamItemInstanceID_t(value);
		}

		// Token: 0x0600097D RID: 2429 RVA: 0x0000D1BB File Offset: 0x0000B3BB
		public static explicit operator ulong(SteamItemInstanceID_t that)
		{
			return that.m_SteamItemInstanceID;
		}

		// Token: 0x0600097E RID: 2430 RVA: 0x0000D197 File Offset: 0x0000B397
		public bool Equals(SteamItemInstanceID_t other)
		{
			return this.m_SteamItemInstanceID == other.m_SteamItemInstanceID;
		}

		// Token: 0x0600097F RID: 2431 RVA: 0x0000D1C3 File Offset: 0x0000B3C3
		public int CompareTo(SteamItemInstanceID_t other)
		{
			return this.m_SteamItemInstanceID.CompareTo(other.m_SteamItemInstanceID);
		}

		// Token: 0x04000A23 RID: 2595
		public static readonly SteamItemInstanceID_t Invalid = new SteamItemInstanceID_t(ulong.MaxValue);

		// Token: 0x04000A24 RID: 2596
		public ulong m_SteamItemInstanceID;
	}
}
