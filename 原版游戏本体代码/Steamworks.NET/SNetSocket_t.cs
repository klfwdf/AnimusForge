using System;

namespace Steamworks
{
	// Token: 0x02000191 RID: 401
	[Serializable]
	public struct SNetSocket_t : IEquatable<SNetSocket_t>, IComparable<SNetSocket_t>
	{
		// Token: 0x060009A0 RID: 2464 RVA: 0x0000D36F File Offset: 0x0000B56F
		public SNetSocket_t(uint value)
		{
			this.m_SNetSocket = value;
		}

		// Token: 0x060009A1 RID: 2465 RVA: 0x0000D378 File Offset: 0x0000B578
		public override string ToString()
		{
			return this.m_SNetSocket.ToString();
		}

		// Token: 0x060009A2 RID: 2466 RVA: 0x0000D385 File Offset: 0x0000B585
		public override bool Equals(object other)
		{
			return other is SNetSocket_t && this == (SNetSocket_t)other;
		}

		// Token: 0x060009A3 RID: 2467 RVA: 0x0000D3A2 File Offset: 0x0000B5A2
		public override int GetHashCode()
		{
			return this.m_SNetSocket.GetHashCode();
		}

		// Token: 0x060009A4 RID: 2468 RVA: 0x0000D3AF File Offset: 0x0000B5AF
		public static bool operator ==(SNetSocket_t x, SNetSocket_t y)
		{
			return x.m_SNetSocket == y.m_SNetSocket;
		}

		// Token: 0x060009A5 RID: 2469 RVA: 0x0000D3BF File Offset: 0x0000B5BF
		public static bool operator !=(SNetSocket_t x, SNetSocket_t y)
		{
			return !(x == y);
		}

		// Token: 0x060009A6 RID: 2470 RVA: 0x0000D3CB File Offset: 0x0000B5CB
		public static explicit operator SNetSocket_t(uint value)
		{
			return new SNetSocket_t(value);
		}

		// Token: 0x060009A7 RID: 2471 RVA: 0x0000D3D3 File Offset: 0x0000B5D3
		public static explicit operator uint(SNetSocket_t that)
		{
			return that.m_SNetSocket;
		}

		// Token: 0x060009A8 RID: 2472 RVA: 0x0000D3AF File Offset: 0x0000B5AF
		public bool Equals(SNetSocket_t other)
		{
			return this.m_SNetSocket == other.m_SNetSocket;
		}

		// Token: 0x060009A9 RID: 2473 RVA: 0x0000D3DB File Offset: 0x0000B5DB
		public int CompareTo(SNetSocket_t other)
		{
			return this.m_SNetSocket.CompareTo(other.m_SNetSocket);
		}

		// Token: 0x04000A2A RID: 2602
		public uint m_SNetSocket;
	}
}
