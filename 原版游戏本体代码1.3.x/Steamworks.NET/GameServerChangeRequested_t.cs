using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000031 RID: 49
	[CallbackIdentity(332)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct GameServerChangeRequested_t
	{
		// Token: 0x17000002 RID: 2
		// (get) Token: 0x0600083B RID: 2107 RVA: 0x0000BCD5 File Offset: 0x00009ED5
		// (set) Token: 0x0600083C RID: 2108 RVA: 0x0000BCE2 File Offset: 0x00009EE2
		public string m_rgchServer
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchServer_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchServer_, 64);
			}
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x0600083D RID: 2109 RVA: 0x0000BCF2 File Offset: 0x00009EF2
		// (set) Token: 0x0600083E RID: 2110 RVA: 0x0000BCFF File Offset: 0x00009EFF
		public string m_rgchPassword
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchPassword_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchPassword_, 64);
			}
		}

		// Token: 0x04000023 RID: 35
		public const int k_iCallback = 332;

		// Token: 0x04000024 RID: 36
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		private byte[] m_rgchServer_;

		// Token: 0x04000025 RID: 37
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
		private byte[] m_rgchPassword_;
	}
}
