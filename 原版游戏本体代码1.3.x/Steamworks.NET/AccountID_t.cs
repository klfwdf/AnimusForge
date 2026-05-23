using System;

namespace Steamworks
{
	// Token: 0x020001A5 RID: 421
	[Serializable]
	public struct AccountID_t : IEquatable<AccountID_t>, IComparable<AccountID_t>
	{
		// Token: 0x06000A4D RID: 2637 RVA: 0x0000DC49 File Offset: 0x0000BE49
		public AccountID_t(uint value)
		{
			this.m_AccountID = value;
		}

		// Token: 0x06000A4E RID: 2638 RVA: 0x0000DC52 File Offset: 0x0000BE52
		public override string ToString()
		{
			return this.m_AccountID.ToString();
		}

		// Token: 0x06000A4F RID: 2639 RVA: 0x0000DC5F File Offset: 0x0000BE5F
		public override bool Equals(object other)
		{
			return other is AccountID_t && this == (AccountID_t)other;
		}

		// Token: 0x06000A50 RID: 2640 RVA: 0x0000DC7C File Offset: 0x0000BE7C
		public override int GetHashCode()
		{
			return this.m_AccountID.GetHashCode();
		}

		// Token: 0x06000A51 RID: 2641 RVA: 0x0000DC89 File Offset: 0x0000BE89
		public static bool operator ==(AccountID_t x, AccountID_t y)
		{
			return x.m_AccountID == y.m_AccountID;
		}

		// Token: 0x06000A52 RID: 2642 RVA: 0x0000DC99 File Offset: 0x0000BE99
		public static bool operator !=(AccountID_t x, AccountID_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A53 RID: 2643 RVA: 0x0000DCA5 File Offset: 0x0000BEA5
		public static explicit operator AccountID_t(uint value)
		{
			return new AccountID_t(value);
		}

		// Token: 0x06000A54 RID: 2644 RVA: 0x0000DCAD File Offset: 0x0000BEAD
		public static explicit operator uint(AccountID_t that)
		{
			return that.m_AccountID;
		}

		// Token: 0x06000A55 RID: 2645 RVA: 0x0000DC89 File Offset: 0x0000BE89
		public bool Equals(AccountID_t other)
		{
			return this.m_AccountID == other.m_AccountID;
		}

		// Token: 0x06000A56 RID: 2646 RVA: 0x0000DCB5 File Offset: 0x0000BEB5
		public int CompareTo(AccountID_t other)
		{
			return this.m_AccountID.CompareTo(other.m_AccountID);
		}

		// Token: 0x04000A59 RID: 2649
		public uint m_AccountID;
	}
}
