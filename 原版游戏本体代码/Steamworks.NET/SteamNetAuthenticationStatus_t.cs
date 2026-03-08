using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000A3 RID: 163
	[CallbackIdentity(1222)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamNetAuthenticationStatus_t
	{
		// Token: 0x1700000A RID: 10
		// (get) Token: 0x0600084B RID: 2123 RVA: 0x0000BDCB File Offset: 0x00009FCB
		// (set) Token: 0x0600084C RID: 2124 RVA: 0x0000BDD8 File Offset: 0x00009FD8
		public string m_debugMsg
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_debugMsg_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_debugMsg_, 256);
			}
		}

		// Token: 0x040001B7 RID: 439
		public const int k_iCallback = 1222;

		// Token: 0x040001B8 RID: 440
		public ESteamNetworkingAvailability m_eAvail;

		// Token: 0x040001B9 RID: 441
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_debugMsg_;
	}
}
