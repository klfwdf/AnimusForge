using System;

namespace Steamworks
{
	// Token: 0x020001A2 RID: 418
	[Serializable]
	public struct UGCFileWriteStreamHandle_t : IEquatable<UGCFileWriteStreamHandle_t>, IComparable<UGCFileWriteStreamHandle_t>
	{
		// Token: 0x06000A2C RID: 2604 RVA: 0x0000DAA3 File Offset: 0x0000BCA3
		public UGCFileWriteStreamHandle_t(ulong value)
		{
			this.m_UGCFileWriteStreamHandle = value;
		}

		// Token: 0x06000A2D RID: 2605 RVA: 0x0000DAAC File Offset: 0x0000BCAC
		public override string ToString()
		{
			return this.m_UGCFileWriteStreamHandle.ToString();
		}

		// Token: 0x06000A2E RID: 2606 RVA: 0x0000DAB9 File Offset: 0x0000BCB9
		public override bool Equals(object other)
		{
			return other is UGCFileWriteStreamHandle_t && this == (UGCFileWriteStreamHandle_t)other;
		}

		// Token: 0x06000A2F RID: 2607 RVA: 0x0000DAD6 File Offset: 0x0000BCD6
		public override int GetHashCode()
		{
			return this.m_UGCFileWriteStreamHandle.GetHashCode();
		}

		// Token: 0x06000A30 RID: 2608 RVA: 0x0000DAE3 File Offset: 0x0000BCE3
		public static bool operator ==(UGCFileWriteStreamHandle_t x, UGCFileWriteStreamHandle_t y)
		{
			return x.m_UGCFileWriteStreamHandle == y.m_UGCFileWriteStreamHandle;
		}

		// Token: 0x06000A31 RID: 2609 RVA: 0x0000DAF3 File Offset: 0x0000BCF3
		public static bool operator !=(UGCFileWriteStreamHandle_t x, UGCFileWriteStreamHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A32 RID: 2610 RVA: 0x0000DAFF File Offset: 0x0000BCFF
		public static explicit operator UGCFileWriteStreamHandle_t(ulong value)
		{
			return new UGCFileWriteStreamHandle_t(value);
		}

		// Token: 0x06000A33 RID: 2611 RVA: 0x0000DB07 File Offset: 0x0000BD07
		public static explicit operator ulong(UGCFileWriteStreamHandle_t that)
		{
			return that.m_UGCFileWriteStreamHandle;
		}

		// Token: 0x06000A34 RID: 2612 RVA: 0x0000DAE3 File Offset: 0x0000BCE3
		public bool Equals(UGCFileWriteStreamHandle_t other)
		{
			return this.m_UGCFileWriteStreamHandle == other.m_UGCFileWriteStreamHandle;
		}

		// Token: 0x06000A35 RID: 2613 RVA: 0x0000DB0F File Offset: 0x0000BD0F
		public int CompareTo(UGCFileWriteStreamHandle_t other)
		{
			return this.m_UGCFileWriteStreamHandle.CompareTo(other.m_UGCFileWriteStreamHandle);
		}

		// Token: 0x04000A53 RID: 2643
		public static readonly UGCFileWriteStreamHandle_t Invalid = new UGCFileWriteStreamHandle_t(ulong.MaxValue);

		// Token: 0x04000A54 RID: 2644
		public ulong m_UGCFileWriteStreamHandle;
	}
}
