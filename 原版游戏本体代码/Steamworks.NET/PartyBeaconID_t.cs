using System;

namespace Steamworks
{
	// Token: 0x020001A8 RID: 424
	[Serializable]
	public struct PartyBeaconID_t : IEquatable<PartyBeaconID_t>, IComparable<PartyBeaconID_t>
	{
		// Token: 0x06000A6D RID: 2669 RVA: 0x0000DDE0 File Offset: 0x0000BFE0
		public PartyBeaconID_t(ulong value)
		{
			this.m_PartyBeaconID = value;
		}

		// Token: 0x06000A6E RID: 2670 RVA: 0x0000DDE9 File Offset: 0x0000BFE9
		public override string ToString()
		{
			return this.m_PartyBeaconID.ToString();
		}

		// Token: 0x06000A6F RID: 2671 RVA: 0x0000DDF6 File Offset: 0x0000BFF6
		public override bool Equals(object other)
		{
			return other is PartyBeaconID_t && this == (PartyBeaconID_t)other;
		}

		// Token: 0x06000A70 RID: 2672 RVA: 0x0000DE13 File Offset: 0x0000C013
		public override int GetHashCode()
		{
			return this.m_PartyBeaconID.GetHashCode();
		}

		// Token: 0x06000A71 RID: 2673 RVA: 0x0000DE20 File Offset: 0x0000C020
		public static bool operator ==(PartyBeaconID_t x, PartyBeaconID_t y)
		{
			return x.m_PartyBeaconID == y.m_PartyBeaconID;
		}

		// Token: 0x06000A72 RID: 2674 RVA: 0x0000DE30 File Offset: 0x0000C030
		public static bool operator !=(PartyBeaconID_t x, PartyBeaconID_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A73 RID: 2675 RVA: 0x0000DE3C File Offset: 0x0000C03C
		public static explicit operator PartyBeaconID_t(ulong value)
		{
			return new PartyBeaconID_t(value);
		}

		// Token: 0x06000A74 RID: 2676 RVA: 0x0000DE44 File Offset: 0x0000C044
		public static explicit operator ulong(PartyBeaconID_t that)
		{
			return that.m_PartyBeaconID;
		}

		// Token: 0x06000A75 RID: 2677 RVA: 0x0000DE20 File Offset: 0x0000C020
		public bool Equals(PartyBeaconID_t other)
		{
			return this.m_PartyBeaconID == other.m_PartyBeaconID;
		}

		// Token: 0x06000A76 RID: 2678 RVA: 0x0000DE4C File Offset: 0x0000C04C
		public int CompareTo(PartyBeaconID_t other)
		{
			return this.m_PartyBeaconID.CompareTo(other.m_PartyBeaconID);
		}

		// Token: 0x04000A5E RID: 2654
		public static readonly PartyBeaconID_t Invalid = new PartyBeaconID_t(0UL);

		// Token: 0x04000A5F RID: 2655
		public ulong m_PartyBeaconID;
	}
}
