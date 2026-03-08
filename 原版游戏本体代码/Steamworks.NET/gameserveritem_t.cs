using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Steamworks
{
	// Token: 0x02000175 RID: 373
	[Serializable]
	[StructLayout(LayoutKind.Sequential, Pack = 4, Size = 372)]
	public class gameserveritem_t
	{
		// Token: 0x06000878 RID: 2168 RVA: 0x0000C09B File Offset: 0x0000A29B
		public string GetGameDir()
		{
			return Encoding.UTF8.GetString(this.m_szGameDir, 0, Array.IndexOf<byte>(this.m_szGameDir, 0));
		}

		// Token: 0x06000879 RID: 2169 RVA: 0x0000C0BA File Offset: 0x0000A2BA
		public void SetGameDir(string dir)
		{
			this.m_szGameDir = Encoding.UTF8.GetBytes(dir + "\0");
		}

		// Token: 0x0600087A RID: 2170 RVA: 0x0000C0D7 File Offset: 0x0000A2D7
		public string GetMap()
		{
			return Encoding.UTF8.GetString(this.m_szMap, 0, Array.IndexOf<byte>(this.m_szMap, 0));
		}

		// Token: 0x0600087B RID: 2171 RVA: 0x0000C0F6 File Offset: 0x0000A2F6
		public void SetMap(string map)
		{
			this.m_szMap = Encoding.UTF8.GetBytes(map + "\0");
		}

		// Token: 0x0600087C RID: 2172 RVA: 0x0000C113 File Offset: 0x0000A313
		public string GetGameDescription()
		{
			return Encoding.UTF8.GetString(this.m_szGameDescription, 0, Array.IndexOf<byte>(this.m_szGameDescription, 0));
		}

		// Token: 0x0600087D RID: 2173 RVA: 0x0000C132 File Offset: 0x0000A332
		public void SetGameDescription(string desc)
		{
			this.m_szGameDescription = Encoding.UTF8.GetBytes(desc + "\0");
		}

		// Token: 0x0600087E RID: 2174 RVA: 0x0000C14F File Offset: 0x0000A34F
		public string GetServerName()
		{
			if (this.m_szServerName[0] == 0)
			{
				return this.m_NetAdr.GetConnectionAddressString();
			}
			return Encoding.UTF8.GetString(this.m_szServerName, 0, Array.IndexOf<byte>(this.m_szServerName, 0));
		}

		// Token: 0x0600087F RID: 2175 RVA: 0x0000C184 File Offset: 0x0000A384
		public void SetServerName(string name)
		{
			this.m_szServerName = Encoding.UTF8.GetBytes(name + "\0");
		}

		// Token: 0x06000880 RID: 2176 RVA: 0x0000C1A1 File Offset: 0x0000A3A1
		public string GetGameTags()
		{
			return Encoding.UTF8.GetString(this.m_szGameTags, 0, Array.IndexOf<byte>(this.m_szGameTags, 0));
		}

		// Token: 0x06000881 RID: 2177 RVA: 0x0000C1C0 File Offset: 0x0000A3C0
		public void SetGameTags(string tags)
		{
			this.m_szGameTags = Encoding.UTF8.GetBytes(tags + "\0");
		}

		// Token: 0x040009E3 RID: 2531
		public servernetadr_t m_NetAdr;

		// Token: 0x040009E4 RID: 2532
		public int m_nPing;

		// Token: 0x040009E5 RID: 2533
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bHadSuccessfulResponse;

		// Token: 0x040009E6 RID: 2534
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bDoNotRefresh;

		// Token: 0x040009E7 RID: 2535
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		private byte[] m_szGameDir;

		// Token: 0x040009E8 RID: 2536
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
		private byte[] m_szMap;

		// Token: 0x040009E9 RID: 2537
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		private byte[] m_szGameDescription;

		// Token: 0x040009EA RID: 2538
		public uint m_nAppID;

		// Token: 0x040009EB RID: 2539
		public int m_nPlayers;

		// Token: 0x040009EC RID: 2540
		public int m_nMaxPlayers;

		// Token: 0x040009ED RID: 2541
		public int m_nBotPlayers;

		// Token: 0x040009EE RID: 2542
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bPassword;

		// Token: 0x040009EF RID: 2543
		[MarshalAs(UnmanagedType.I1)]
		public bool m_bSecure;

		// Token: 0x040009F0 RID: 2544
		public uint m_ulTimeLastPlayed;

		// Token: 0x040009F1 RID: 2545
		public int m_nServerVersion;

		// Token: 0x040009F2 RID: 2546
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		private byte[] m_szServerName;

		// Token: 0x040009F3 RID: 2547
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		private byte[] m_szGameTags;

		// Token: 0x040009F4 RID: 2548
		public CSteamID m_steamID;
	}
}
