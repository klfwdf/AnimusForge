using System;

namespace Steamworks
{
	// Token: 0x020001AF RID: 431
	[Serializable]
	public struct SteamLeaderboardEntries_t : IEquatable<SteamLeaderboardEntries_t>, IComparable<SteamLeaderboardEntries_t>
	{
		// Token: 0x06000AB2 RID: 2738 RVA: 0x0000E2F3 File Offset: 0x0000C4F3
		public SteamLeaderboardEntries_t(ulong value)
		{
			this.m_SteamLeaderboardEntries = value;
		}

		// Token: 0x06000AB3 RID: 2739 RVA: 0x0000E2FC File Offset: 0x0000C4FC
		public override string ToString()
		{
			return this.m_SteamLeaderboardEntries.ToString();
		}

		// Token: 0x06000AB4 RID: 2740 RVA: 0x0000E309 File Offset: 0x0000C509
		public override bool Equals(object other)
		{
			return other is SteamLeaderboardEntries_t && this == (SteamLeaderboardEntries_t)other;
		}

		// Token: 0x06000AB5 RID: 2741 RVA: 0x0000E326 File Offset: 0x0000C526
		public override int GetHashCode()
		{
			return this.m_SteamLeaderboardEntries.GetHashCode();
		}

		// Token: 0x06000AB6 RID: 2742 RVA: 0x0000E333 File Offset: 0x0000C533
		public static bool operator ==(SteamLeaderboardEntries_t x, SteamLeaderboardEntries_t y)
		{
			return x.m_SteamLeaderboardEntries == y.m_SteamLeaderboardEntries;
		}

		// Token: 0x06000AB7 RID: 2743 RVA: 0x0000E343 File Offset: 0x0000C543
		public static bool operator !=(SteamLeaderboardEntries_t x, SteamLeaderboardEntries_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000AB8 RID: 2744 RVA: 0x0000E34F File Offset: 0x0000C54F
		public static explicit operator SteamLeaderboardEntries_t(ulong value)
		{
			return new SteamLeaderboardEntries_t(value);
		}

		// Token: 0x06000AB9 RID: 2745 RVA: 0x0000E357 File Offset: 0x0000C557
		public static explicit operator ulong(SteamLeaderboardEntries_t that)
		{
			return that.m_SteamLeaderboardEntries;
		}

		// Token: 0x06000ABA RID: 2746 RVA: 0x0000E333 File Offset: 0x0000C533
		public bool Equals(SteamLeaderboardEntries_t other)
		{
			return this.m_SteamLeaderboardEntries == other.m_SteamLeaderboardEntries;
		}

		// Token: 0x06000ABB RID: 2747 RVA: 0x0000E35F File Offset: 0x0000C55F
		public int CompareTo(SteamLeaderboardEntries_t other)
		{
			return this.m_SteamLeaderboardEntries.CompareTo(other.m_SteamLeaderboardEntries);
		}

		// Token: 0x04000A6B RID: 2667
		public ulong m_SteamLeaderboardEntries;
	}
}
