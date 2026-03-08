using System;

namespace Steamworks
{
	// Token: 0x0200018B RID: 395
	[Serializable]
	public struct SteamInventoryUpdateHandle_t : IEquatable<SteamInventoryUpdateHandle_t>, IComparable<SteamInventoryUpdateHandle_t>
	{
		// Token: 0x06000961 RID: 2401 RVA: 0x0000D04B File Offset: 0x0000B24B
		public SteamInventoryUpdateHandle_t(ulong value)
		{
			this.m_SteamInventoryUpdateHandle = value;
		}

		// Token: 0x06000962 RID: 2402 RVA: 0x0000D054 File Offset: 0x0000B254
		public override string ToString()
		{
			return this.m_SteamInventoryUpdateHandle.ToString();
		}

		// Token: 0x06000963 RID: 2403 RVA: 0x0000D061 File Offset: 0x0000B261
		public override bool Equals(object other)
		{
			return other is SteamInventoryUpdateHandle_t && this == (SteamInventoryUpdateHandle_t)other;
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x0000D07E File Offset: 0x0000B27E
		public override int GetHashCode()
		{
			return this.m_SteamInventoryUpdateHandle.GetHashCode();
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x0000D08B File Offset: 0x0000B28B
		public static bool operator ==(SteamInventoryUpdateHandle_t x, SteamInventoryUpdateHandle_t y)
		{
			return x.m_SteamInventoryUpdateHandle == y.m_SteamInventoryUpdateHandle;
		}

		// Token: 0x06000966 RID: 2406 RVA: 0x0000D09B File Offset: 0x0000B29B
		public static bool operator !=(SteamInventoryUpdateHandle_t x, SteamInventoryUpdateHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x0000D0A7 File Offset: 0x0000B2A7
		public static explicit operator SteamInventoryUpdateHandle_t(ulong value)
		{
			return new SteamInventoryUpdateHandle_t(value);
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x0000D0AF File Offset: 0x0000B2AF
		public static explicit operator ulong(SteamInventoryUpdateHandle_t that)
		{
			return that.m_SteamInventoryUpdateHandle;
		}

		// Token: 0x06000969 RID: 2409 RVA: 0x0000D08B File Offset: 0x0000B28B
		public bool Equals(SteamInventoryUpdateHandle_t other)
		{
			return this.m_SteamInventoryUpdateHandle == other.m_SteamInventoryUpdateHandle;
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x0000D0B7 File Offset: 0x0000B2B7
		public int CompareTo(SteamInventoryUpdateHandle_t other)
		{
			return this.m_SteamInventoryUpdateHandle.CompareTo(other.m_SteamInventoryUpdateHandle);
		}

		// Token: 0x04000A20 RID: 2592
		public static readonly SteamInventoryUpdateHandle_t Invalid = new SteamInventoryUpdateHandle_t(ulong.MaxValue);

		// Token: 0x04000A21 RID: 2593
		public ulong m_SteamInventoryUpdateHandle;
	}
}
