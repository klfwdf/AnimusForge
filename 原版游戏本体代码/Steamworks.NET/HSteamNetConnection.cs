using System;

namespace Steamworks
{
	// Token: 0x02000196 RID: 406
	[Serializable]
	public struct HSteamNetConnection : IEquatable<HSteamNetConnection>, IComparable<HSteamNetConnection>
	{
		// Token: 0x060009BD RID: 2493 RVA: 0x0000D4A5 File Offset: 0x0000B6A5
		public HSteamNetConnection(uint value)
		{
			this.m_HSteamNetConnection = value;
		}

		// Token: 0x060009BE RID: 2494 RVA: 0x0000D4AE File Offset: 0x0000B6AE
		public override string ToString()
		{
			return this.m_HSteamNetConnection.ToString();
		}

		// Token: 0x060009BF RID: 2495 RVA: 0x0000D4BB File Offset: 0x0000B6BB
		public override bool Equals(object other)
		{
			return other is HSteamNetConnection && this == (HSteamNetConnection)other;
		}

		// Token: 0x060009C0 RID: 2496 RVA: 0x0000D4D8 File Offset: 0x0000B6D8
		public override int GetHashCode()
		{
			return this.m_HSteamNetConnection.GetHashCode();
		}

		// Token: 0x060009C1 RID: 2497 RVA: 0x0000D4E5 File Offset: 0x0000B6E5
		public static bool operator ==(HSteamNetConnection x, HSteamNetConnection y)
		{
			return x.m_HSteamNetConnection == y.m_HSteamNetConnection;
		}

		// Token: 0x060009C2 RID: 2498 RVA: 0x0000D4F5 File Offset: 0x0000B6F5
		public static bool operator !=(HSteamNetConnection x, HSteamNetConnection y)
		{
			return !(x == y);
		}

		// Token: 0x060009C3 RID: 2499 RVA: 0x0000D501 File Offset: 0x0000B701
		public static explicit operator HSteamNetConnection(uint value)
		{
			return new HSteamNetConnection(value);
		}

		// Token: 0x060009C4 RID: 2500 RVA: 0x0000D509 File Offset: 0x0000B709
		public static explicit operator uint(HSteamNetConnection that)
		{
			return that.m_HSteamNetConnection;
		}

		// Token: 0x060009C5 RID: 2501 RVA: 0x0000D4E5 File Offset: 0x0000B6E5
		public bool Equals(HSteamNetConnection other)
		{
			return this.m_HSteamNetConnection == other.m_HSteamNetConnection;
		}

		// Token: 0x060009C6 RID: 2502 RVA: 0x0000D511 File Offset: 0x0000B711
		public int CompareTo(HSteamNetConnection other)
		{
			return this.m_HSteamNetConnection.CompareTo(other.m_HSteamNetConnection);
		}

		// Token: 0x04000A2D RID: 2605
		public static readonly HSteamNetConnection Invalid = new HSteamNetConnection(0U);

		// Token: 0x04000A2E RID: 2606
		public uint m_HSteamNetConnection;
	}
}
