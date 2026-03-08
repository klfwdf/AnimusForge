using System;

namespace Steamworks
{
	// Token: 0x020001A0 RID: 416
	[Serializable]
	public struct PublishedFileId_t : IEquatable<PublishedFileId_t>, IComparable<PublishedFileId_t>
	{
		// Token: 0x06000A16 RID: 2582 RVA: 0x0000D989 File Offset: 0x0000BB89
		public PublishedFileId_t(ulong value)
		{
			this.m_PublishedFileId = value;
		}

		// Token: 0x06000A17 RID: 2583 RVA: 0x0000D992 File Offset: 0x0000BB92
		public override string ToString()
		{
			return this.m_PublishedFileId.ToString();
		}

		// Token: 0x06000A18 RID: 2584 RVA: 0x0000D99F File Offset: 0x0000BB9F
		public override bool Equals(object other)
		{
			return other is PublishedFileId_t && this == (PublishedFileId_t)other;
		}

		// Token: 0x06000A19 RID: 2585 RVA: 0x0000D9BC File Offset: 0x0000BBBC
		public override int GetHashCode()
		{
			return this.m_PublishedFileId.GetHashCode();
		}

		// Token: 0x06000A1A RID: 2586 RVA: 0x0000D9C9 File Offset: 0x0000BBC9
		public static bool operator ==(PublishedFileId_t x, PublishedFileId_t y)
		{
			return x.m_PublishedFileId == y.m_PublishedFileId;
		}

		// Token: 0x06000A1B RID: 2587 RVA: 0x0000D9D9 File Offset: 0x0000BBD9
		public static bool operator !=(PublishedFileId_t x, PublishedFileId_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A1C RID: 2588 RVA: 0x0000D9E5 File Offset: 0x0000BBE5
		public static explicit operator PublishedFileId_t(ulong value)
		{
			return new PublishedFileId_t(value);
		}

		// Token: 0x06000A1D RID: 2589 RVA: 0x0000D9ED File Offset: 0x0000BBED
		public static explicit operator ulong(PublishedFileId_t that)
		{
			return that.m_PublishedFileId;
		}

		// Token: 0x06000A1E RID: 2590 RVA: 0x0000D9C9 File Offset: 0x0000BBC9
		public bool Equals(PublishedFileId_t other)
		{
			return this.m_PublishedFileId == other.m_PublishedFileId;
		}

		// Token: 0x06000A1F RID: 2591 RVA: 0x0000D9F5 File Offset: 0x0000BBF5
		public int CompareTo(PublishedFileId_t other)
		{
			return this.m_PublishedFileId.CompareTo(other.m_PublishedFileId);
		}

		// Token: 0x04000A4F RID: 2639
		public static readonly PublishedFileId_t Invalid = new PublishedFileId_t(0UL);

		// Token: 0x04000A50 RID: 2640
		public ulong m_PublishedFileId;
	}
}
