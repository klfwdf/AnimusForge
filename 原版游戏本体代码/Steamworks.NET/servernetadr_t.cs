using System;

namespace Steamworks
{
	// Token: 0x02000176 RID: 374
	[Serializable]
	public struct servernetadr_t
	{
		// Token: 0x06000883 RID: 2179 RVA: 0x0000C1E5 File Offset: 0x0000A3E5
		public void Init(uint ip, ushort usQueryPort, ushort usConnectionPort)
		{
			this.m_unIP = ip;
			this.m_usQueryPort = usQueryPort;
			this.m_usConnectionPort = usConnectionPort;
		}

		// Token: 0x06000884 RID: 2180 RVA: 0x0000C1FC File Offset: 0x0000A3FC
		public ushort GetQueryPort()
		{
			return this.m_usQueryPort;
		}

		// Token: 0x06000885 RID: 2181 RVA: 0x0000C204 File Offset: 0x0000A404
		public void SetQueryPort(ushort usPort)
		{
			this.m_usQueryPort = usPort;
		}

		// Token: 0x06000886 RID: 2182 RVA: 0x0000C20D File Offset: 0x0000A40D
		public ushort GetConnectionPort()
		{
			return this.m_usConnectionPort;
		}

		// Token: 0x06000887 RID: 2183 RVA: 0x0000C215 File Offset: 0x0000A415
		public void SetConnectionPort(ushort usPort)
		{
			this.m_usConnectionPort = usPort;
		}

		// Token: 0x06000888 RID: 2184 RVA: 0x0000C21E File Offset: 0x0000A41E
		public uint GetIP()
		{
			return this.m_unIP;
		}

		// Token: 0x06000889 RID: 2185 RVA: 0x0000C226 File Offset: 0x0000A426
		public void SetIP(uint unIP)
		{
			this.m_unIP = unIP;
		}

		// Token: 0x0600088A RID: 2186 RVA: 0x0000C22F File Offset: 0x0000A42F
		public string GetConnectionAddressString()
		{
			return servernetadr_t.ToString(this.m_unIP, this.m_usConnectionPort);
		}

		// Token: 0x0600088B RID: 2187 RVA: 0x0000C242 File Offset: 0x0000A442
		public string GetQueryAddressString()
		{
			return servernetadr_t.ToString(this.m_unIP, this.m_usQueryPort);
		}

		// Token: 0x0600088C RID: 2188 RVA: 0x0000C258 File Offset: 0x0000A458
		public static string ToString(uint unIP, ushort usPort)
		{
			return string.Format("{0}.{1}.{2}.{3}:{4}", new object[]
			{
				(ulong)(unIP >> 24) & 255UL,
				(ulong)(unIP >> 16) & 255UL,
				(ulong)(unIP >> 8) & 255UL,
				(ulong)unIP & 255UL,
				usPort
			});
		}

		// Token: 0x0600088D RID: 2189 RVA: 0x0000C2CA File Offset: 0x0000A4CA
		public static bool operator <(servernetadr_t x, servernetadr_t y)
		{
			return x.m_unIP < y.m_unIP || (x.m_unIP == y.m_unIP && x.m_usQueryPort < y.m_usQueryPort);
		}

		// Token: 0x0600088E RID: 2190 RVA: 0x0000C2FA File Offset: 0x0000A4FA
		public static bool operator >(servernetadr_t x, servernetadr_t y)
		{
			return x.m_unIP > y.m_unIP || (x.m_unIP == y.m_unIP && x.m_usQueryPort > y.m_usQueryPort);
		}

		// Token: 0x0600088F RID: 2191 RVA: 0x0000C32A File Offset: 0x0000A52A
		public override bool Equals(object other)
		{
			return other is servernetadr_t && this == (servernetadr_t)other;
		}

		// Token: 0x06000890 RID: 2192 RVA: 0x0000C347 File Offset: 0x0000A547
		public override int GetHashCode()
		{
			return this.m_unIP.GetHashCode() + this.m_usQueryPort.GetHashCode() + this.m_usConnectionPort.GetHashCode();
		}

		// Token: 0x06000891 RID: 2193 RVA: 0x0000C36C File Offset: 0x0000A56C
		public static bool operator ==(servernetadr_t x, servernetadr_t y)
		{
			return x.m_unIP == y.m_unIP && x.m_usQueryPort == y.m_usQueryPort && x.m_usConnectionPort == y.m_usConnectionPort;
		}

		// Token: 0x06000892 RID: 2194 RVA: 0x0000C39A File Offset: 0x0000A59A
		public static bool operator !=(servernetadr_t x, servernetadr_t y)
		{
			return !(x == y);
		}

		// Token: 0x06000893 RID: 2195 RVA: 0x0000C36C File Offset: 0x0000A56C
		public bool Equals(servernetadr_t other)
		{
			return this.m_unIP == other.m_unIP && this.m_usQueryPort == other.m_usQueryPort && this.m_usConnectionPort == other.m_usConnectionPort;
		}

		// Token: 0x06000894 RID: 2196 RVA: 0x0000C3A6 File Offset: 0x0000A5A6
		public int CompareTo(servernetadr_t other)
		{
			return this.m_unIP.CompareTo(other.m_unIP) + this.m_usQueryPort.CompareTo(other.m_usQueryPort) + this.m_usConnectionPort.CompareTo(other.m_usConnectionPort);
		}

		// Token: 0x040009F5 RID: 2549
		private ushort m_usConnectionPort;

		// Token: 0x040009F6 RID: 2550
		private ushort m_usQueryPort;

		// Token: 0x040009F7 RID: 2551
		private uint m_unIP;
	}
}
