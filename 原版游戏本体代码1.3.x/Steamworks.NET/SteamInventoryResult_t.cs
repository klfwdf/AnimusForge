using System;

namespace Steamworks
{
	// Token: 0x0200018A RID: 394
	[Serializable]
	public struct SteamInventoryResult_t : IEquatable<SteamInventoryResult_t>, IComparable<SteamInventoryResult_t>
	{
		// Token: 0x06000956 RID: 2390 RVA: 0x0000CFBF File Offset: 0x0000B1BF
		public SteamInventoryResult_t(int value)
		{
			this.m_SteamInventoryResult = value;
		}

		// Token: 0x06000957 RID: 2391 RVA: 0x0000CFC8 File Offset: 0x0000B1C8
		public override string ToString()
		{
			return this.m_SteamInventoryResult.ToString();
		}

		// Token: 0x06000958 RID: 2392 RVA: 0x0000CFD5 File Offset: 0x0000B1D5
		public override bool Equals(object other)
		{
			return other is SteamInventoryResult_t && this == (SteamInventoryResult_t)other;
		}

		// Token: 0x06000959 RID: 2393 RVA: 0x0000CFF2 File Offset: 0x0000B1F2
		public override int GetHashCode()
		{
			return this.m_SteamInventoryResult.GetHashCode();
		}

		// Token: 0x0600095A RID: 2394 RVA: 0x0000CFFF File Offset: 0x0000B1FF
		public static bool operator ==(SteamInventoryResult_t x, SteamInventoryResult_t y)
		{
			return x.m_SteamInventoryResult == y.m_SteamInventoryResult;
		}

		// Token: 0x0600095B RID: 2395 RVA: 0x0000D00F File Offset: 0x0000B20F
		public static bool operator !=(SteamInventoryResult_t x, SteamInventoryResult_t y)
		{
			return !(x == y);
		}

		// Token: 0x0600095C RID: 2396 RVA: 0x0000D01B File Offset: 0x0000B21B
		public static explicit operator SteamInventoryResult_t(int value)
		{
			return new SteamInventoryResult_t(value);
		}

		// Token: 0x0600095D RID: 2397 RVA: 0x0000D023 File Offset: 0x0000B223
		public static explicit operator int(SteamInventoryResult_t that)
		{
			return that.m_SteamInventoryResult;
		}

		// Token: 0x0600095E RID: 2398 RVA: 0x0000CFFF File Offset: 0x0000B1FF
		public bool Equals(SteamInventoryResult_t other)
		{
			return this.m_SteamInventoryResult == other.m_SteamInventoryResult;
		}

		// Token: 0x0600095F RID: 2399 RVA: 0x0000D02B File Offset: 0x0000B22B
		public int CompareTo(SteamInventoryResult_t other)
		{
			return this.m_SteamInventoryResult.CompareTo(other.m_SteamInventoryResult);
		}

		// Token: 0x04000A1E RID: 2590
		public static readonly SteamInventoryResult_t Invalid = new SteamInventoryResult_t(-1);

		// Token: 0x04000A1F RID: 2591
		public int m_SteamInventoryResult;
	}
}
