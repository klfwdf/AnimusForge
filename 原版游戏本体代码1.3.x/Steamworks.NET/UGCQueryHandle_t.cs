using System;

namespace Steamworks
{
	// Token: 0x020001AC RID: 428
	[Serializable]
	public struct UGCQueryHandle_t : IEquatable<UGCQueryHandle_t>, IComparable<UGCQueryHandle_t>
	{
		// Token: 0x06000A92 RID: 2706 RVA: 0x0000E15A File Offset: 0x0000C35A
		public UGCQueryHandle_t(ulong value)
		{
			this.m_UGCQueryHandle = value;
		}

		// Token: 0x06000A93 RID: 2707 RVA: 0x0000E163 File Offset: 0x0000C363
		public override string ToString()
		{
			return this.m_UGCQueryHandle.ToString();
		}

		// Token: 0x06000A94 RID: 2708 RVA: 0x0000E170 File Offset: 0x0000C370
		public override bool Equals(object other)
		{
			return other is UGCQueryHandle_t && this == (UGCQueryHandle_t)other;
		}

		// Token: 0x06000A95 RID: 2709 RVA: 0x0000E18D File Offset: 0x0000C38D
		public override int GetHashCode()
		{
			return this.m_UGCQueryHandle.GetHashCode();
		}

		// Token: 0x06000A96 RID: 2710 RVA: 0x0000E19A File Offset: 0x0000C39A
		public static bool operator ==(UGCQueryHandle_t x, UGCQueryHandle_t y)
		{
			return x.m_UGCQueryHandle == y.m_UGCQueryHandle;
		}

		// Token: 0x06000A97 RID: 2711 RVA: 0x0000E1AA File Offset: 0x0000C3AA
		public static bool operator !=(UGCQueryHandle_t x, UGCQueryHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A98 RID: 2712 RVA: 0x0000E1B6 File Offset: 0x0000C3B6
		public static explicit operator UGCQueryHandle_t(ulong value)
		{
			return new UGCQueryHandle_t(value);
		}

		// Token: 0x06000A99 RID: 2713 RVA: 0x0000E1BE File Offset: 0x0000C3BE
		public static explicit operator ulong(UGCQueryHandle_t that)
		{
			return that.m_UGCQueryHandle;
		}

		// Token: 0x06000A9A RID: 2714 RVA: 0x0000E19A File Offset: 0x0000C39A
		public bool Equals(UGCQueryHandle_t other)
		{
			return this.m_UGCQueryHandle == other.m_UGCQueryHandle;
		}

		// Token: 0x06000A9B RID: 2715 RVA: 0x0000E1C6 File Offset: 0x0000C3C6
		public int CompareTo(UGCQueryHandle_t other)
		{
			return this.m_UGCQueryHandle.CompareTo(other.m_UGCQueryHandle);
		}

		// Token: 0x04000A66 RID: 2662
		public static readonly UGCQueryHandle_t Invalid = new UGCQueryHandle_t(ulong.MaxValue);

		// Token: 0x04000A67 RID: 2663
		public ulong m_UGCQueryHandle;
	}
}
