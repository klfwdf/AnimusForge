using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000F8 RID: 248
	[CallbackIdentity(4611)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GetVideoURLResult_t
	{
		// Token: 0x17000018 RID: 24
		// (get) Token: 0x06000867 RID: 2151 RVA: 0x0000BF8B File Offset: 0x0000A18B
		// (set) Token: 0x06000868 RID: 2152 RVA: 0x0000BF98 File Offset: 0x0000A198
		public string m_rgchURL
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchURL_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchURL_, 256);
			}
		}

		// Token: 0x040002FD RID: 765
		public const int k_iCallback = 4611;

		// Token: 0x040002FE RID: 766
		public EResult m_eResult;

		// Token: 0x040002FF RID: 767
		public AppId_t m_unVideoAppID;

		// Token: 0x04000300 RID: 768
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_rgchURL_;
	}
}
