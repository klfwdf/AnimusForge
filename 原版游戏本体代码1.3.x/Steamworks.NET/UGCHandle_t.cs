using System;

namespace Steamworks
{
	// Token: 0x020001A3 RID: 419
	[Serializable]
	public struct UGCHandle_t : IEquatable<UGCHandle_t>, IComparable<UGCHandle_t>
	{
		// Token: 0x06000A37 RID: 2615 RVA: 0x0000DB30 File Offset: 0x0000BD30
		public UGCHandle_t(ulong value)
		{
			this.m_UGCHandle = value;
		}

		// Token: 0x06000A38 RID: 2616 RVA: 0x0000DB39 File Offset: 0x0000BD39
		public override string ToString()
		{
			return this.m_UGCHandle.ToString();
		}

		// Token: 0x06000A39 RID: 2617 RVA: 0x0000DB46 File Offset: 0x0000BD46
		public override bool Equals(object other)
		{
			return other is UGCHandle_t && this == (UGCHandle_t)other;
		}

		// Token: 0x06000A3A RID: 2618 RVA: 0x0000DB63 File Offset: 0x0000BD63
		public override int GetHashCode()
		{
			return this.m_UGCHandle.GetHashCode();
		}

		// Token: 0x06000A3B RID: 2619 RVA: 0x0000DB70 File Offset: 0x0000BD70
		public static bool operator ==(UGCHandle_t x, UGCHandle_t y)
		{
			return x.m_UGCHandle == y.m_UGCHandle;
		}

		// Token: 0x06000A3C RID: 2620 RVA: 0x0000DB80 File Offset: 0x0000BD80
		public static bool operator !=(UGCHandle_t x, UGCHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A3D RID: 2621 RVA: 0x0000DB8C File Offset: 0x0000BD8C
		public static explicit operator UGCHandle_t(ulong value)
		{
			return new UGCHandle_t(value);
		}

		// Token: 0x06000A3E RID: 2622 RVA: 0x0000DB94 File Offset: 0x0000BD94
		public static explicit operator ulong(UGCHandle_t that)
		{
			return that.m_UGCHandle;
		}

		// Token: 0x06000A3F RID: 2623 RVA: 0x0000DB70 File Offset: 0x0000BD70
		public bool Equals(UGCHandle_t other)
		{
			return this.m_UGCHandle == other.m_UGCHandle;
		}

		// Token: 0x06000A40 RID: 2624 RVA: 0x0000DB9C File Offset: 0x0000BD9C
		public int CompareTo(UGCHandle_t other)
		{
			return this.m_UGCHandle.CompareTo(other.m_UGCHandle);
		}

		// Token: 0x04000A55 RID: 2645
		public static readonly UGCHandle_t Invalid = new UGCHandle_t(ulong.MaxValue);

		// Token: 0x04000A56 RID: 2646
		public ulong m_UGCHandle;
	}
}
