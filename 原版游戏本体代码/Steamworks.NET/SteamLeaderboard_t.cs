using System;

namespace Steamworks
{
	// Token: 0x020001AE RID: 430
	[Serializable]
	public struct SteamLeaderboard_t : IEquatable<SteamLeaderboard_t>, IComparable<SteamLeaderboard_t>
	{
		// Token: 0x06000AA8 RID: 2728 RVA: 0x0000E274 File Offset: 0x0000C474
		public SteamLeaderboard_t(ulong value)
		{
			this.m_SteamLeaderboard = value;
		}

		// Token: 0x06000AA9 RID: 2729 RVA: 0x0000E27D File Offset: 0x0000C47D
		public override string ToString()
		{
			return this.m_SteamLeaderboard.ToString();
		}

		// Token: 0x06000AAA RID: 2730 RVA: 0x0000E28A File Offset: 0x0000C48A
		public override bool Equals(object other)
		{
			return other is SteamLeaderboard_t && this == (SteamLeaderboard_t)other;
		}

		// Token: 0x06000AAB RID: 2731 RVA: 0x0000E2A7 File Offset: 0x0000C4A7
		public override int GetHashCode()
		{
			return this.m_SteamLeaderboard.GetHashCode();
		}

		// Token: 0x06000AAC RID: 2732 RVA: 0x0000E2B4 File Offset: 0x0000C4B4
		public static bool operator ==(SteamLeaderboard_t x, SteamLeaderboard_t y)
		{
			return x.m_SteamLeaderboard == y.m_SteamLeaderboard;
		}

		// Token: 0x06000AAD RID: 2733 RVA: 0x0000E2C4 File Offset: 0x0000C4C4
		public static bool operator !=(SteamLeaderboard_t x, SteamLeaderboard_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000AAE RID: 2734 RVA: 0x0000E2D0 File Offset: 0x0000C4D0
		public static explicit operator SteamLeaderboard_t(ulong value)
		{
			return new SteamLeaderboard_t(value);
		}

		// Token: 0x06000AAF RID: 2735 RVA: 0x0000E2D8 File Offset: 0x0000C4D8
		public static explicit operator ulong(SteamLeaderboard_t that)
		{
			return that.m_SteamLeaderboard;
		}

		// Token: 0x06000AB0 RID: 2736 RVA: 0x0000E2B4 File Offset: 0x0000C4B4
		public bool Equals(SteamLeaderboard_t other)
		{
			return this.m_SteamLeaderboard == other.m_SteamLeaderboard;
		}

		// Token: 0x06000AB1 RID: 2737 RVA: 0x0000E2E0 File Offset: 0x0000C4E0
		public int CompareTo(SteamLeaderboard_t other)
		{
			return this.m_SteamLeaderboard.CompareTo(other.m_SteamLeaderboard);
		}

		// Token: 0x04000A6A RID: 2666
		public ulong m_SteamLeaderboard;
	}
}
