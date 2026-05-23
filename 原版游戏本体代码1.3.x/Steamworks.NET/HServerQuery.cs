using System;

namespace Steamworks
{
	// Token: 0x0200018F RID: 399
	[Serializable]
	public struct HServerQuery : IEquatable<HServerQuery>, IComparable<HServerQuery>
	{
		// Token: 0x0600098B RID: 2443 RVA: 0x0000D264 File Offset: 0x0000B464
		public HServerQuery(int value)
		{
			this.m_HServerQuery = value;
		}

		// Token: 0x0600098C RID: 2444 RVA: 0x0000D26D File Offset: 0x0000B46D
		public override string ToString()
		{
			return this.m_HServerQuery.ToString();
		}

		// Token: 0x0600098D RID: 2445 RVA: 0x0000D27A File Offset: 0x0000B47A
		public override bool Equals(object other)
		{
			return other is HServerQuery && this == (HServerQuery)other;
		}

		// Token: 0x0600098E RID: 2446 RVA: 0x0000D297 File Offset: 0x0000B497
		public override int GetHashCode()
		{
			return this.m_HServerQuery.GetHashCode();
		}

		// Token: 0x0600098F RID: 2447 RVA: 0x0000D2A4 File Offset: 0x0000B4A4
		public static bool operator ==(HServerQuery x, HServerQuery y)
		{
			return x.m_HServerQuery == y.m_HServerQuery;
		}

		// Token: 0x06000990 RID: 2448 RVA: 0x0000D2B4 File Offset: 0x0000B4B4
		public static bool operator !=(HServerQuery x, HServerQuery y)
		{
			return !(x == y);
		}

		// Token: 0x06000991 RID: 2449 RVA: 0x0000D2C0 File Offset: 0x0000B4C0
		public static explicit operator HServerQuery(int value)
		{
			return new HServerQuery(value);
		}

		// Token: 0x06000992 RID: 2450 RVA: 0x0000D2C8 File Offset: 0x0000B4C8
		public static explicit operator int(HServerQuery that)
		{
			return that.m_HServerQuery;
		}

		// Token: 0x06000993 RID: 2451 RVA: 0x0000D2A4 File Offset: 0x0000B4A4
		public bool Equals(HServerQuery other)
		{
			return this.m_HServerQuery == other.m_HServerQuery;
		}

		// Token: 0x06000994 RID: 2452 RVA: 0x0000D2D0 File Offset: 0x0000B4D0
		public int CompareTo(HServerQuery other)
		{
			return this.m_HServerQuery.CompareTo(other.m_HServerQuery);
		}

		// Token: 0x04000A27 RID: 2599
		public static readonly HServerQuery Invalid = new HServerQuery(-1);

		// Token: 0x04000A28 RID: 2600
		public int m_HServerQuery;
	}
}
