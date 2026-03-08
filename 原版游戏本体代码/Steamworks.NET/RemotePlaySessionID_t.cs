using System;

namespace Steamworks
{
	// Token: 0x0200019F RID: 415
	[Serializable]
	public struct RemotePlaySessionID_t : IEquatable<RemotePlaySessionID_t>, IComparable<RemotePlaySessionID_t>
	{
		// Token: 0x06000A0C RID: 2572 RVA: 0x0000D90A File Offset: 0x0000BB0A
		public RemotePlaySessionID_t(uint value)
		{
			this.m_RemotePlaySessionID = value;
		}

		// Token: 0x06000A0D RID: 2573 RVA: 0x0000D913 File Offset: 0x0000BB13
		public override string ToString()
		{
			return this.m_RemotePlaySessionID.ToString();
		}

		// Token: 0x06000A0E RID: 2574 RVA: 0x0000D920 File Offset: 0x0000BB20
		public override bool Equals(object other)
		{
			return other is RemotePlaySessionID_t && this == (RemotePlaySessionID_t)other;
		}

		// Token: 0x06000A0F RID: 2575 RVA: 0x0000D93D File Offset: 0x0000BB3D
		public override int GetHashCode()
		{
			return this.m_RemotePlaySessionID.GetHashCode();
		}

		// Token: 0x06000A10 RID: 2576 RVA: 0x0000D94A File Offset: 0x0000BB4A
		public static bool operator ==(RemotePlaySessionID_t x, RemotePlaySessionID_t y)
		{
			return x.m_RemotePlaySessionID == y.m_RemotePlaySessionID;
		}

		// Token: 0x06000A11 RID: 2577 RVA: 0x0000D95A File Offset: 0x0000BB5A
		public static bool operator !=(RemotePlaySessionID_t x, RemotePlaySessionID_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000A12 RID: 2578 RVA: 0x0000D966 File Offset: 0x0000BB66
		public static explicit operator RemotePlaySessionID_t(uint value)
		{
			return new RemotePlaySessionID_t(value);
		}

		// Token: 0x06000A13 RID: 2579 RVA: 0x0000D96E File Offset: 0x0000BB6E
		public static explicit operator uint(RemotePlaySessionID_t that)
		{
			return that.m_RemotePlaySessionID;
		}

		// Token: 0x06000A14 RID: 2580 RVA: 0x0000D94A File Offset: 0x0000BB4A
		public bool Equals(RemotePlaySessionID_t other)
		{
			return this.m_RemotePlaySessionID == other.m_RemotePlaySessionID;
		}

		// Token: 0x06000A15 RID: 2581 RVA: 0x0000D976 File Offset: 0x0000BB76
		public int CompareTo(RemotePlaySessionID_t other)
		{
			return this.m_RemotePlaySessionID.CompareTo(other.m_RemotePlaySessionID);
		}

		// Token: 0x04000A4E RID: 2638
		public uint m_RemotePlaySessionID;
	}
}
