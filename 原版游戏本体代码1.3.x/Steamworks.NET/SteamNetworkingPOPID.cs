using System;

namespace Steamworks
{
	// Token: 0x0200019E RID: 414
	[Serializable]
	public struct SteamNetworkingPOPID : IEquatable<SteamNetworkingPOPID>, IComparable<SteamNetworkingPOPID>
	{
		// Token: 0x06000A02 RID: 2562 RVA: 0x0000D88B File Offset: 0x0000BA8B
		public SteamNetworkingPOPID(uint value)
		{
			this.m_SteamNetworkingPOPID = value;
		}

		// Token: 0x06000A03 RID: 2563 RVA: 0x0000D894 File Offset: 0x0000BA94
		public override string ToString()
		{
			return this.m_SteamNetworkingPOPID.ToString();
		}

		// Token: 0x06000A04 RID: 2564 RVA: 0x0000D8A1 File Offset: 0x0000BAA1
		public override bool Equals(object other)
		{
			return other is SteamNetworkingPOPID && this == (SteamNetworkingPOPID)other;
		}

		// Token: 0x06000A05 RID: 2565 RVA: 0x0000D8BE File Offset: 0x0000BABE
		public override int GetHashCode()
		{
			return this.m_SteamNetworkingPOPID.GetHashCode();
		}

		// Token: 0x06000A06 RID: 2566 RVA: 0x0000D8CB File Offset: 0x0000BACB
		public static bool operator ==(SteamNetworkingPOPID x, SteamNetworkingPOPID y)
		{
			return x.m_SteamNetworkingPOPID == y.m_SteamNetworkingPOPID;
		}

		// Token: 0x06000A07 RID: 2567 RVA: 0x0000D8DB File Offset: 0x0000BADB
		public static bool operator !=(SteamNetworkingPOPID x, SteamNetworkingPOPID y)
		{
			return !(x == y);
		}

		// Token: 0x06000A08 RID: 2568 RVA: 0x0000D8E7 File Offset: 0x0000BAE7
		public static explicit operator SteamNetworkingPOPID(uint value)
		{
			return new SteamNetworkingPOPID(value);
		}

		// Token: 0x06000A09 RID: 2569 RVA: 0x0000D8EF File Offset: 0x0000BAEF
		public static explicit operator uint(SteamNetworkingPOPID that)
		{
			return that.m_SteamNetworkingPOPID;
		}

		// Token: 0x06000A0A RID: 2570 RVA: 0x0000D8CB File Offset: 0x0000BACB
		public bool Equals(SteamNetworkingPOPID other)
		{
			return this.m_SteamNetworkingPOPID == other.m_SteamNetworkingPOPID;
		}

		// Token: 0x06000A0B RID: 2571 RVA: 0x0000D8F7 File Offset: 0x0000BAF7
		public int CompareTo(SteamNetworkingPOPID other)
		{
			return this.m_SteamNetworkingPOPID.CompareTo(other.m_SteamNetworkingPOPID);
		}

		// Token: 0x04000A4D RID: 2637
		public uint m_SteamNetworkingPOPID;
	}
}
