using System;

namespace Steamworks
{
	// Token: 0x02000195 RID: 405
	[Serializable]
	public struct HSteamListenSocket : IEquatable<HSteamListenSocket>, IComparable<HSteamListenSocket>
	{
		// Token: 0x060009B2 RID: 2482 RVA: 0x0000D419 File Offset: 0x0000B619
		public HSteamListenSocket(uint value)
		{
			this.m_HSteamListenSocket = value;
		}

		// Token: 0x060009B3 RID: 2483 RVA: 0x0000D422 File Offset: 0x0000B622
		public override string ToString()
		{
			return this.m_HSteamListenSocket.ToString();
		}

		// Token: 0x060009B4 RID: 2484 RVA: 0x0000D42F File Offset: 0x0000B62F
		public override bool Equals(object other)
		{
			return other is HSteamListenSocket && this == (HSteamListenSocket)other;
		}

		// Token: 0x060009B5 RID: 2485 RVA: 0x0000D44C File Offset: 0x0000B64C
		public override int GetHashCode()
		{
			return this.m_HSteamListenSocket.GetHashCode();
		}

		// Token: 0x060009B6 RID: 2486 RVA: 0x0000D459 File Offset: 0x0000B659
		public static bool operator ==(HSteamListenSocket x, HSteamListenSocket y)
		{
			return x.m_HSteamListenSocket == y.m_HSteamListenSocket;
		}

		// Token: 0x060009B7 RID: 2487 RVA: 0x0000D469 File Offset: 0x0000B669
		public static bool operator !=(HSteamListenSocket x, HSteamListenSocket y)
		{
			return !(x == y);
		}

		// Token: 0x060009B8 RID: 2488 RVA: 0x0000D475 File Offset: 0x0000B675
		public static explicit operator HSteamListenSocket(uint value)
		{
			return new HSteamListenSocket(value);
		}

		// Token: 0x060009B9 RID: 2489 RVA: 0x0000D47D File Offset: 0x0000B67D
		public static explicit operator uint(HSteamListenSocket that)
		{
			return that.m_HSteamListenSocket;
		}

		// Token: 0x060009BA RID: 2490 RVA: 0x0000D459 File Offset: 0x0000B659
		public bool Equals(HSteamListenSocket other)
		{
			return this.m_HSteamListenSocket == other.m_HSteamListenSocket;
		}

		// Token: 0x060009BB RID: 2491 RVA: 0x0000D485 File Offset: 0x0000B685
		public int CompareTo(HSteamListenSocket other)
		{
			return this.m_HSteamListenSocket.CompareTo(other.m_HSteamListenSocket);
		}

		// Token: 0x04000A2B RID: 2603
		public static readonly HSteamListenSocket Invalid = new HSteamListenSocket(0U);

		// Token: 0x04000A2C RID: 2604
		public uint m_HSteamListenSocket;
	}
}
