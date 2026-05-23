using System;

namespace Steamworks
{
	// Token: 0x02000190 RID: 400
	[Serializable]
	public struct SNetListenSocket_t : IEquatable<SNetListenSocket_t>, IComparable<SNetListenSocket_t>
	{
		// Token: 0x06000996 RID: 2454 RVA: 0x0000D2F0 File Offset: 0x0000B4F0
		public SNetListenSocket_t(uint value)
		{
			this.m_SNetListenSocket = value;
		}

		// Token: 0x06000997 RID: 2455 RVA: 0x0000D2F9 File Offset: 0x0000B4F9
		public override string ToString()
		{
			return this.m_SNetListenSocket.ToString();
		}

		// Token: 0x06000998 RID: 2456 RVA: 0x0000D306 File Offset: 0x0000B506
		public override bool Equals(object other)
		{
			return other is SNetListenSocket_t && this == (SNetListenSocket_t)other;
		}

		// Token: 0x06000999 RID: 2457 RVA: 0x0000D323 File Offset: 0x0000B523
		public override int GetHashCode()
		{
			return this.m_SNetListenSocket.GetHashCode();
		}

		// Token: 0x0600099A RID: 2458 RVA: 0x0000D330 File Offset: 0x0000B530
		public static bool operator ==(SNetListenSocket_t x, SNetListenSocket_t y)
		{
			return x.m_SNetListenSocket == y.m_SNetListenSocket;
		}

		// Token: 0x0600099B RID: 2459 RVA: 0x0000D340 File Offset: 0x0000B540
		public static bool operator !=(SNetListenSocket_t x, SNetListenSocket_t y)
		{
			return !(x == y);
		}

		// Token: 0x0600099C RID: 2460 RVA: 0x0000D34C File Offset: 0x0000B54C
		public static explicit operator SNetListenSocket_t(uint value)
		{
			return new SNetListenSocket_t(value);
		}

		// Token: 0x0600099D RID: 2461 RVA: 0x0000D354 File Offset: 0x0000B554
		public static explicit operator uint(SNetListenSocket_t that)
		{
			return that.m_SNetListenSocket;
		}

		// Token: 0x0600099E RID: 2462 RVA: 0x0000D330 File Offset: 0x0000B530
		public bool Equals(SNetListenSocket_t other)
		{
			return this.m_SNetListenSocket == other.m_SNetListenSocket;
		}

		// Token: 0x0600099F RID: 2463 RVA: 0x0000D35C File Offset: 0x0000B55C
		public int CompareTo(SNetListenSocket_t other)
		{
			return this.m_SNetListenSocket.CompareTo(other.m_SNetListenSocket);
		}

		// Token: 0x04000A29 RID: 2601
		public uint m_SNetListenSocket;
	}
}
