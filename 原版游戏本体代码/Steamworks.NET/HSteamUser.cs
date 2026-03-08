using System;

namespace Steamworks
{
	// Token: 0x02000178 RID: 376
	[Serializable]
	public struct HSteamUser : IEquatable<HSteamUser>, IComparable<HSteamUser>
	{
		// Token: 0x0600089F RID: 2207 RVA: 0x0000C45C File Offset: 0x0000A65C
		public HSteamUser(int value)
		{
			this.m_HSteamUser = value;
		}

		// Token: 0x060008A0 RID: 2208 RVA: 0x0000C465 File Offset: 0x0000A665
		public override string ToString()
		{
			return this.m_HSteamUser.ToString();
		}

		// Token: 0x060008A1 RID: 2209 RVA: 0x0000C472 File Offset: 0x0000A672
		public override bool Equals(object other)
		{
			return other is HSteamUser && this == (HSteamUser)other;
		}

		// Token: 0x060008A2 RID: 2210 RVA: 0x0000C48F File Offset: 0x0000A68F
		public override int GetHashCode()
		{
			return this.m_HSteamUser.GetHashCode();
		}

		// Token: 0x060008A3 RID: 2211 RVA: 0x0000C49C File Offset: 0x0000A69C
		public static bool operator ==(HSteamUser x, HSteamUser y)
		{
			return x.m_HSteamUser == y.m_HSteamUser;
		}

		// Token: 0x060008A4 RID: 2212 RVA: 0x0000C4AC File Offset: 0x0000A6AC
		public static bool operator !=(HSteamUser x, HSteamUser y)
		{
			return !(x == y);
		}

		// Token: 0x060008A5 RID: 2213 RVA: 0x0000C4B8 File Offset: 0x0000A6B8
		public static explicit operator HSteamUser(int value)
		{
			return new HSteamUser(value);
		}

		// Token: 0x060008A6 RID: 2214 RVA: 0x0000C4C0 File Offset: 0x0000A6C0
		public static explicit operator int(HSteamUser that)
		{
			return that.m_HSteamUser;
		}

		// Token: 0x060008A7 RID: 2215 RVA: 0x0000C49C File Offset: 0x0000A69C
		public bool Equals(HSteamUser other)
		{
			return this.m_HSteamUser == other.m_HSteamUser;
		}

		// Token: 0x060008A8 RID: 2216 RVA: 0x0000C4C8 File Offset: 0x0000A6C8
		public int CompareTo(HSteamUser other)
		{
			return this.m_HSteamUser.CompareTo(other.m_HSteamUser);
		}

		// Token: 0x040009F9 RID: 2553
		public int m_HSteamUser;
	}
}
