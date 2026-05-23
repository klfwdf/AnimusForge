using System;

namespace Steamworks
{
	// Token: 0x0200019D RID: 413
	[Serializable]
	public struct SteamNetworkingMicroseconds : IEquatable<SteamNetworkingMicroseconds>, IComparable<SteamNetworkingMicroseconds>
	{
		// Token: 0x060009F8 RID: 2552 RVA: 0x0000D80C File Offset: 0x0000BA0C
		public SteamNetworkingMicroseconds(long value)
		{
			this.m_SteamNetworkingMicroseconds = value;
		}

		// Token: 0x060009F9 RID: 2553 RVA: 0x0000D815 File Offset: 0x0000BA15
		public override string ToString()
		{
			return this.m_SteamNetworkingMicroseconds.ToString();
		}

		// Token: 0x060009FA RID: 2554 RVA: 0x0000D822 File Offset: 0x0000BA22
		public override bool Equals(object other)
		{
			return other is SteamNetworkingMicroseconds && this == (SteamNetworkingMicroseconds)other;
		}

		// Token: 0x060009FB RID: 2555 RVA: 0x0000D83F File Offset: 0x0000BA3F
		public override int GetHashCode()
		{
			return this.m_SteamNetworkingMicroseconds.GetHashCode();
		}

		// Token: 0x060009FC RID: 2556 RVA: 0x0000D84C File Offset: 0x0000BA4C
		public static bool operator ==(SteamNetworkingMicroseconds x, SteamNetworkingMicroseconds y)
		{
			return x.m_SteamNetworkingMicroseconds == y.m_SteamNetworkingMicroseconds;
		}

		// Token: 0x060009FD RID: 2557 RVA: 0x0000D85C File Offset: 0x0000BA5C
		public static bool operator !=(SteamNetworkingMicroseconds x, SteamNetworkingMicroseconds y)
		{
			return !(x == y);
		}

		// Token: 0x060009FE RID: 2558 RVA: 0x0000D868 File Offset: 0x0000BA68
		public static explicit operator SteamNetworkingMicroseconds(long value)
		{
			return new SteamNetworkingMicroseconds(value);
		}

		// Token: 0x060009FF RID: 2559 RVA: 0x0000D870 File Offset: 0x0000BA70
		public static explicit operator long(SteamNetworkingMicroseconds that)
		{
			return that.m_SteamNetworkingMicroseconds;
		}

		// Token: 0x06000A00 RID: 2560 RVA: 0x0000D84C File Offset: 0x0000BA4C
		public bool Equals(SteamNetworkingMicroseconds other)
		{
			return this.m_SteamNetworkingMicroseconds == other.m_SteamNetworkingMicroseconds;
		}

		// Token: 0x06000A01 RID: 2561 RVA: 0x0000D878 File Offset: 0x0000BA78
		public int CompareTo(SteamNetworkingMicroseconds other)
		{
			return this.m_SteamNetworkingMicroseconds.CompareTo(other.m_SteamNetworkingMicroseconds);
		}

		// Token: 0x04000A4C RID: 2636
		public long m_SteamNetworkingMicroseconds;
	}
}
