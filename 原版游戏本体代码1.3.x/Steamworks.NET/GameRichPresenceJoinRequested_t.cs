using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000036 RID: 54
	[CallbackIdentity(337)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GameRichPresenceJoinRequested_t
	{
		// Token: 0x17000004 RID: 4
		// (get) Token: 0x0600083F RID: 2111 RVA: 0x0000BD0F File Offset: 0x00009F0F
		// (set) Token: 0x06000840 RID: 2112 RVA: 0x0000BD1C File Offset: 0x00009F1C
		public string m_rgchConnect
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchConnect_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchConnect_, 256);
			}
		}

		// Token: 0x04000035 RID: 53
		public const int k_iCallback = 337;

		// Token: 0x04000036 RID: 54
		public CSteamID m_steamIDFriend;

		// Token: 0x04000037 RID: 55
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_rgchConnect_;
	}
}
