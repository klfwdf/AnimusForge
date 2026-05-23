using System;

namespace Steamworks
{
	// Token: 0x020001A7 RID: 423
	[Serializable]
	public struct DepotId_t : IEquatable<DepotId_t>, IComparable<DepotId_t>
	{
		// Token: 0x06000A62 RID: 2658 RVA: 0x0000DD54 File Offset: 0x0000BF54
		public DepotId_t(uint value)
		{
			this.m_DepotId = value;
		}

		// Token: 0x06000A63 RID: 2659 RVA: 0x0000DD5D File Offset: 0x0000BF5D
		public override string ToString()
		{
			return this.m_DepotId.ToString();
		}

		// Token: 0x06000A64 RID: 2660 RVA: 0x0000DD6A File Offset: 0x0000BF6A
		public override bool Equals(object other)
		{
			return other is DepotId_t && this == (DepotId_t)other;
		}

		// Token: 0x06000A65 RID: 2661 RVA: 0x0000DD87 File Offset: 0x0000BF87
		public override int GetHashCode()
		{
			return this.m_DepotId.GetHashCode();
		}

		// Token: 0x06000A66 RID: 2662 RVA: 0x0000DD94 File Offset: 0x0000BF94
		public static bool operator ==(DepotId_t x, DepotId_t y)
		{
			return x.m_DepotId == y.m_DepotId;
		}

		// Token: 0x06000A67 RID: 2663 RVA: 0x0000DDA4 File Offset: 0x0000BFA4
		public static bool operator !=(DepotId_t x, DepotId_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A68 RID: 2664 RVA: 0x0000DDB0 File Offset: 0x0000BFB0
		public static explicit operator DepotId_t(uint value)
		{
			return new DepotId_t(value);
		}

		// Token: 0x06000A69 RID: 2665 RVA: 0x0000DDB8 File Offset: 0x0000BFB8
		public static explicit operator uint(DepotId_t that)
		{
			return that.m_DepotId;
		}

		// Token: 0x06000A6A RID: 2666 RVA: 0x0000DD94 File Offset: 0x0000BF94
		public bool Equals(DepotId_t other)
		{
			return this.m_DepotId == other.m_DepotId;
		}

		// Token: 0x06000A6B RID: 2667 RVA: 0x0000DDC0 File Offset: 0x0000BFC0
		public int CompareTo(DepotId_t other)
		{
			return this.m_DepotId.CompareTo(other.m_DepotId);
		}

		// Token: 0x04000A5C RID: 2652
		public static readonly DepotId_t Invalid = new DepotId_t(0U);

		// Token: 0x04000A5D RID: 2653
		public uint m_DepotId;
	}
}
