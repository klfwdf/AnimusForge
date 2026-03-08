using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000E0 RID: 224
	[CallbackIdentity(164)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GameWebCallback_t
	{
		// Token: 0x17000014 RID: 20
		// (get) Token: 0x0600085F RID: 2143 RVA: 0x0000BF0B File Offset: 0x0000A10B
		// (set) Token: 0x06000860 RID: 2144 RVA: 0x0000BF18 File Offset: 0x0000A118
		public string m_szURL
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_szURL_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_szURL_, 256);
			}
		}

		// Token: 0x040002AD RID: 685
		public const int k_iCallback = 164;

		// Token: 0x040002AE RID: 686
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
		private byte[] m_szURL_;
	}
}
