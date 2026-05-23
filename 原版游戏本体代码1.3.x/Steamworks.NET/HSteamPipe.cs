using System;

namespace Steamworks
{
	// Token: 0x02000177 RID: 375
	[Serializable]
	public struct HSteamPipe : IEquatable<HSteamPipe>, IComparable<HSteamPipe>
	{
		// Token: 0x06000895 RID: 2197 RVA: 0x0000C3DD File Offset: 0x0000A5DD
		public HSteamPipe(int value)
		{
			this.m_HSteamPipe = value;
		}

		// Token: 0x06000896 RID: 2198 RVA: 0x0000C3E6 File Offset: 0x0000A5E6
		public override string ToString()
		{
			return this.m_HSteamPipe.ToString();
		}

		// Token: 0x06000897 RID: 2199 RVA: 0x0000C3F3 File Offset: 0x0000A5F3
		public override bool Equals(object other)
		{
			return other is HSteamPipe && this == (HSteamPipe)other;
		}

		// Token: 0x06000898 RID: 2200 RVA: 0x0000C410 File Offset: 0x0000A610
		public override int GetHashCode()
		{
			return this.m_HSteamPipe.GetHashCode();
		}

		// Token: 0x06000899 RID: 2201 RVA: 0x0000C41D File Offset: 0x0000A61D
		public static bool operator ==(HSteamPipe x, HSteamPipe y)
		{
			return x.m_HSteamPipe == y.m_HSteamPipe;
		}

		// Token: 0x0600089A RID: 2202 RVA: 0x0000C42D File Offset: 0x0000A62D
		public static bool operator !=(HSteamPipe x, HSteamPipe y)
		{
			return !(x == y);
		}

		// Token: 0x0600089B RID: 2203 RVA: 0x0000C439 File Offset: 0x0000A639
		public static explicit operator HSteamPipe(int value)
		{
			return new HSteamPipe(value);
		}

		// Token: 0x0600089C RID: 2204 RVA: 0x0000C441 File Offset: 0x0000A641
		public static explicit operator int(HSteamPipe that)
		{
			return that.m_HSteamPipe;
		}

		// Token: 0x0600089D RID: 2205 RVA: 0x0000C41D File Offset: 0x0000A61D
		public bool Equals(HSteamPipe other)
		{
			return this.m_HSteamPipe == other.m_HSteamPipe;
		}

		// Token: 0x0600089E RID: 2206 RVA: 0x0000C449 File Offset: 0x0000A649
		public int CompareTo(HSteamPipe other)
		{
			return this.m_HSteamPipe.CompareTo(other.m_HSteamPipe);
		}

		// Token: 0x040009F8 RID: 2552
		public int m_HSteamPipe;
	}
}
