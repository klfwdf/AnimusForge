using System;

namespace Steamworks
{
	// Token: 0x02000197 RID: 407
	[Serializable]
	public struct HSteamNetPollGroup : IEquatable<HSteamNetPollGroup>, IComparable<HSteamNetPollGroup>
	{
		// Token: 0x060009C8 RID: 2504 RVA: 0x0000D531 File Offset: 0x0000B731
		public HSteamNetPollGroup(uint value)
		{
			this.m_HSteamNetPollGroup = value;
		}

		// Token: 0x060009C9 RID: 2505 RVA: 0x0000D53A File Offset: 0x0000B73A
		public override string ToString()
		{
			return this.m_HSteamNetPollGroup.ToString();
		}

		// Token: 0x060009CA RID: 2506 RVA: 0x0000D547 File Offset: 0x0000B747
		public override bool Equals(object other)
		{
			return other is HSteamNetPollGroup && this == (HSteamNetPollGroup)other;
		}

		// Token: 0x060009CB RID: 2507 RVA: 0x0000D564 File Offset: 0x0000B764
		public override int GetHashCode()
		{
			return this.m_HSteamNetPollGroup.GetHashCode();
		}

		// Token: 0x060009CC RID: 2508 RVA: 0x0000D571 File Offset: 0x0000B771
		public static bool operator ==(HSteamNetPollGroup x, HSteamNetPollGroup y)
		{
			return x.m_HSteamNetPollGroup == y.m_HSteamNetPollGroup;
		}

		// Token: 0x060009CD RID: 2509 RVA: 0x0000D581 File Offset: 0x0000B781
		public static bool operator !=(HSteamNetPollGroup x, HSteamNetPollGroup y)
		{
			return !(x == y);
		}

		// Token: 0x060009CE RID: 2510 RVA: 0x0000D58D File Offset: 0x0000B78D
		public static explicit operator HSteamNetPollGroup(uint value)
		{
			return new HSteamNetPollGroup(value);
		}

		// Token: 0x060009CF RID: 2511 RVA: 0x0000D595 File Offset: 0x0000B795
		public static explicit operator uint(HSteamNetPollGroup that)
		{
			return that.m_HSteamNetPollGroup;
		}

		// Token: 0x060009D0 RID: 2512 RVA: 0x0000D571 File Offset: 0x0000B771
		public bool Equals(HSteamNetPollGroup other)
		{
			return this.m_HSteamNetPollGroup == other.m_HSteamNetPollGroup;
		}

		// Token: 0x060009D1 RID: 2513 RVA: 0x0000D59D File Offset: 0x0000B79D
		public int CompareTo(HSteamNetPollGroup other)
		{
			return this.m_HSteamNetPollGroup.CompareTo(other.m_HSteamNetPollGroup);
		}

		// Token: 0x04000A2F RID: 2607
		public static readonly HSteamNetPollGroup Invalid = new HSteamNetPollGroup(0U);

		// Token: 0x04000A30 RID: 2608
		public uint m_HSteamNetPollGroup;
	}
}
