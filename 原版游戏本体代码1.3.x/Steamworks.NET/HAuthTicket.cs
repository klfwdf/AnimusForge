using System;

namespace Steamworks
{
	// Token: 0x0200017D RID: 381
	[Serializable]
	public struct HAuthTicket : IEquatable<HAuthTicket>, IComparable<HAuthTicket>
	{
		// Token: 0x060008F1 RID: 2289 RVA: 0x0000CAEC File Offset: 0x0000ACEC
		public HAuthTicket(uint value)
		{
			this.m_HAuthTicket = value;
		}

		// Token: 0x060008F2 RID: 2290 RVA: 0x0000CAF5 File Offset: 0x0000ACF5
		public override string ToString()
		{
			return this.m_HAuthTicket.ToString();
		}

		// Token: 0x060008F3 RID: 2291 RVA: 0x0000CB02 File Offset: 0x0000AD02
		public override bool Equals(object other)
		{
			return other is HAuthTicket && this == (HAuthTicket)other;
		}

		// Token: 0x060008F4 RID: 2292 RVA: 0x0000CB1F File Offset: 0x0000AD1F
		public override int GetHashCode()
		{
			return this.m_HAuthTicket.GetHashCode();
		}

		// Token: 0x060008F5 RID: 2293 RVA: 0x0000CB2C File Offset: 0x0000AD2C
		public static bool operator ==(HAuthTicket x, HAuthTicket y)
		{
			return x.m_HAuthTicket == y.m_HAuthTicket;
		}

		// Token: 0x060008F6 RID: 2294 RVA: 0x0000CB3C File Offset: 0x0000AD3C
		public static bool operator !=(HAuthTicket x, HAuthTicket y)
		{
			return !(x == y);
		}

		// Token: 0x060008F7 RID: 2295 RVA: 0x0000CB48 File Offset: 0x0000AD48
		public static explicit operator HAuthTicket(uint value)
		{
			return new HAuthTicket(value);
		}

		// Token: 0x060008F8 RID: 2296 RVA: 0x0000CB50 File Offset: 0x0000AD50
		public static explicit operator uint(HAuthTicket that)
		{
			return that.m_HAuthTicket;
		}

		// Token: 0x060008F9 RID: 2297 RVA: 0x0000CB2C File Offset: 0x0000AD2C
		public bool Equals(HAuthTicket other)
		{
			return this.m_HAuthTicket == other.m_HAuthTicket;
		}

		// Token: 0x060008FA RID: 2298 RVA: 0x0000CB58 File Offset: 0x0000AD58
		public int CompareTo(HAuthTicket other)
		{
			return this.m_HAuthTicket.CompareTo(other.m_HAuthTicket);
		}

		// Token: 0x04000A01 RID: 2561
		public static readonly HAuthTicket Invalid = new HAuthTicket(0U);

		// Token: 0x04000A02 RID: 2562
		public uint m_HAuthTicket;
	}
}
