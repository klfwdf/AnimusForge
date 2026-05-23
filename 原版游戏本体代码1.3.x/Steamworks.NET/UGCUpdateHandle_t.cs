using System;

namespace Steamworks
{
	// Token: 0x020001AD RID: 429
	[Serializable]
	public struct UGCUpdateHandle_t : IEquatable<UGCUpdateHandle_t>, IComparable<UGCUpdateHandle_t>
	{
		// Token: 0x06000A9D RID: 2717 RVA: 0x0000E1E7 File Offset: 0x0000C3E7
		public UGCUpdateHandle_t(ulong value)
		{
			this.m_UGCUpdateHandle = value;
		}

		// Token: 0x06000A9E RID: 2718 RVA: 0x0000E1F0 File Offset: 0x0000C3F0
		public override string ToString()
		{
			return this.m_UGCUpdateHandle.ToString();
		}

		// Token: 0x06000A9F RID: 2719 RVA: 0x0000E1FD File Offset: 0x0000C3FD
		public override bool Equals(object other)
		{
			return other is UGCUpdateHandle_t && this == (UGCUpdateHandle_t)other;
		}

		// Token: 0x06000AA0 RID: 2720 RVA: 0x0000E21A File Offset: 0x0000C41A
		public override int GetHashCode()
		{
			return this.m_UGCUpdateHandle.GetHashCode();
		}

		// Token: 0x06000AA1 RID: 2721 RVA: 0x0000E227 File Offset: 0x0000C427
		public static bool operator ==(UGCUpdateHandle_t x, UGCUpdateHandle_t y)
		{
			return x.m_UGCUpdateHandle == y.m_UGCUpdateHandle;
		}

		// Token: 0x06000AA2 RID: 2722 RVA: 0x0000E237 File Offset: 0x0000C437
		public static bool operator !=(UGCUpdateHandle_t x, UGCUpdateHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000AA3 RID: 2723 RVA: 0x0000E243 File Offset: 0x0000C443
		public static explicit operator UGCUpdateHandle_t(ulong value)
		{
			return new UGCUpdateHandle_t(value);
		}

		// Token: 0x06000AA4 RID: 2724 RVA: 0x0000E24B File Offset: 0x0000C44B
		public static explicit operator ulong(UGCUpdateHandle_t that)
		{
			return that.m_UGCUpdateHandle;
		}

		// Token: 0x06000AA5 RID: 2725 RVA: 0x0000E227 File Offset: 0x0000C427
		public bool Equals(UGCUpdateHandle_t other)
		{
			return this.m_UGCUpdateHandle == other.m_UGCUpdateHandle;
		}

		// Token: 0x06000AA6 RID: 2726 RVA: 0x0000E253 File Offset: 0x0000C453
		public int CompareTo(UGCUpdateHandle_t other)
		{
			return this.m_UGCUpdateHandle.CompareTo(other.m_UGCUpdateHandle);
		}

		// Token: 0x04000A68 RID: 2664
		public static readonly UGCUpdateHandle_t Invalid = new UGCUpdateHandle_t(ulong.MaxValue);

		// Token: 0x04000A69 RID: 2665
		public ulong m_UGCUpdateHandle;
	}
}
