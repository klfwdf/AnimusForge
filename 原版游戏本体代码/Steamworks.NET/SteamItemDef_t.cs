using System;

namespace Steamworks
{
	// Token: 0x0200018C RID: 396
	[Serializable]
	public struct SteamItemDef_t : IEquatable<SteamItemDef_t>, IComparable<SteamItemDef_t>
	{
		// Token: 0x0600096C RID: 2412 RVA: 0x0000D0D8 File Offset: 0x0000B2D8
		public SteamItemDef_t(int value)
		{
			this.m_SteamItemDef = value;
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x0000D0E1 File Offset: 0x0000B2E1
		public override string ToString()
		{
			return this.m_SteamItemDef.ToString();
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x0000D0EE File Offset: 0x0000B2EE
		public override bool Equals(object other)
		{
			return other is SteamItemDef_t && this == (SteamItemDef_t)other;
		}

		// Token: 0x0600096F RID: 2415 RVA: 0x0000D10B File Offset: 0x0000B30B
		public override int GetHashCode()
		{
			return this.m_SteamItemDef.GetHashCode();
		}

		// Token: 0x06000970 RID: 2416 RVA: 0x0000D118 File Offset: 0x0000B318
		public static bool operator ==(SteamItemDef_t x, SteamItemDef_t y)
		{
			return x.m_SteamItemDef == y.m_SteamItemDef;
		}

		// Token: 0x06000971 RID: 2417 RVA: 0x0000D128 File Offset: 0x0000B328
		public static bool operator !=(SteamItemDef_t x, SteamItemDef_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000972 RID: 2418 RVA: 0x0000D134 File Offset: 0x0000B334
		public static explicit operator SteamItemDef_t(int value)
		{
			return new SteamItemDef_t(value);
		}

		// Token: 0x06000973 RID: 2419 RVA: 0x0000D13C File Offset: 0x0000B33C
		public static explicit operator int(SteamItemDef_t that)
		{
			return that.m_SteamItemDef;
		}

		// Token: 0x06000974 RID: 2420 RVA: 0x0000D118 File Offset: 0x0000B318
		public bool Equals(SteamItemDef_t other)
		{
			return this.m_SteamItemDef == other.m_SteamItemDef;
		}

		// Token: 0x06000975 RID: 2421 RVA: 0x0000D144 File Offset: 0x0000B344
		public int CompareTo(SteamItemDef_t other)
		{
			return this.m_SteamItemDef.CompareTo(other.m_SteamItemDef);
		}

		// Token: 0x04000A22 RID: 2594
		public int m_SteamItemDef;
	}
}
