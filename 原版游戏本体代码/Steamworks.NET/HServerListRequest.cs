using System;

namespace Steamworks
{
	// Token: 0x0200018E RID: 398
	[Serializable]
	public struct HServerListRequest : IEquatable<HServerListRequest>
	{
		// Token: 0x06000981 RID: 2433 RVA: 0x0000D1E4 File Offset: 0x0000B3E4
		public HServerListRequest(IntPtr value)
		{
			this.m_HServerListRequest = value;
		}

		// Token: 0x06000982 RID: 2434 RVA: 0x0000D1ED File Offset: 0x0000B3ED
		public override string ToString()
		{
			return this.m_HServerListRequest.ToString();
		}

		// Token: 0x06000983 RID: 2435 RVA: 0x0000D1FA File Offset: 0x0000B3FA
		public override bool Equals(object other)
		{
			return other is HServerListRequest && this == (HServerListRequest)other;
		}

		// Token: 0x06000984 RID: 2436 RVA: 0x0000D217 File Offset: 0x0000B417
		public override int GetHashCode()
		{
			return this.m_HServerListRequest.GetHashCode();
		}

		// Token: 0x06000985 RID: 2437 RVA: 0x0000D224 File Offset: 0x0000B424
		public static bool operator ==(HServerListRequest x, HServerListRequest y)
		{
			return x.m_HServerListRequest == y.m_HServerListRequest;
		}

		// Token: 0x06000986 RID: 2438 RVA: 0x0000D237 File Offset: 0x0000B437
		public static bool operator !=(HServerListRequest x, HServerListRequest y)
		{
			return !(x == y);
		}

		// Token: 0x06000987 RID: 2439 RVA: 0x0000D243 File Offset: 0x0000B443
		public static explicit operator HServerListRequest(IntPtr value)
		{
			return new HServerListRequest(value);
		}

		// Token: 0x06000988 RID: 2440 RVA: 0x0000D24B File Offset: 0x0000B44B
		public static explicit operator IntPtr(HServerListRequest that)
		{
			return that.m_HServerListRequest;
		}

		// Token: 0x06000989 RID: 2441 RVA: 0x0000D224 File Offset: 0x0000B424
		public bool Equals(HServerListRequest other)
		{
			return this.m_HServerListRequest == other.m_HServerListRequest;
		}

		// Token: 0x04000A25 RID: 2597
		public static readonly HServerListRequest Invalid = new HServerListRequest(IntPtr.Zero);

		// Token: 0x04000A26 RID: 2598
		public IntPtr m_HServerListRequest;
	}
}
