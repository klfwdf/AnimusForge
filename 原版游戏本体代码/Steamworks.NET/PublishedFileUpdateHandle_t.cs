using System;

namespace Steamworks
{
	// Token: 0x020001A1 RID: 417
	[Serializable]
	public struct PublishedFileUpdateHandle_t : IEquatable<PublishedFileUpdateHandle_t>, IComparable<PublishedFileUpdateHandle_t>
	{
		// Token: 0x06000A21 RID: 2593 RVA: 0x0000DA16 File Offset: 0x0000BC16
		public PublishedFileUpdateHandle_t(ulong value)
		{
			this.m_PublishedFileUpdateHandle = value;
		}

		// Token: 0x06000A22 RID: 2594 RVA: 0x0000DA1F File Offset: 0x0000BC1F
		public override string ToString()
		{
			return this.m_PublishedFileUpdateHandle.ToString();
		}

		// Token: 0x06000A23 RID: 2595 RVA: 0x0000DA2C File Offset: 0x0000BC2C
		public override bool Equals(object other)
		{
			return other is PublishedFileUpdateHandle_t && this == (PublishedFileUpdateHandle_t)other;
		}

		// Token: 0x06000A24 RID: 2596 RVA: 0x0000DA49 File Offset: 0x0000BC49
		public override int GetHashCode()
		{
			return this.m_PublishedFileUpdateHandle.GetHashCode();
		}

		// Token: 0x06000A25 RID: 2597 RVA: 0x0000DA56 File Offset: 0x0000BC56
		public static bool operator ==(PublishedFileUpdateHandle_t x, PublishedFileUpdateHandle_t y)
		{
			return x.m_PublishedFileUpdateHandle == y.m_PublishedFileUpdateHandle;
		}

		// Token: 0x06000A26 RID: 2598 RVA: 0x0000DA66 File Offset: 0x0000BC66
		public static bool operator !=(PublishedFileUpdateHandle_t x, PublishedFileUpdateHandle_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A27 RID: 2599 RVA: 0x0000DA72 File Offset: 0x0000BC72
		public static explicit operator PublishedFileUpdateHandle_t(ulong value)
		{
			return new PublishedFileUpdateHandle_t(value);
		}

		// Token: 0x06000A28 RID: 2600 RVA: 0x0000DA7A File Offset: 0x0000BC7A
		public static explicit operator ulong(PublishedFileUpdateHandle_t that)
		{
			return that.m_PublishedFileUpdateHandle;
		}

		// Token: 0x06000A29 RID: 2601 RVA: 0x0000DA56 File Offset: 0x0000BC56
		public bool Equals(PublishedFileUpdateHandle_t other)
		{
			return this.m_PublishedFileUpdateHandle == other.m_PublishedFileUpdateHandle;
		}

		// Token: 0x06000A2A RID: 2602 RVA: 0x0000DA82 File Offset: 0x0000BC82
		public int CompareTo(PublishedFileUpdateHandle_t other)
		{
			return this.m_PublishedFileUpdateHandle.CompareTo(other.m_PublishedFileUpdateHandle);
		}

		// Token: 0x04000A51 RID: 2641
		public static readonly PublishedFileUpdateHandle_t Invalid = new PublishedFileUpdateHandle_t(ulong.MaxValue);

		// Token: 0x04000A52 RID: 2642
		public ulong m_PublishedFileUpdateHandle;
	}
}
