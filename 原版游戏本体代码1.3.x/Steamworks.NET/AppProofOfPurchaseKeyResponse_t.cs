using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200002C RID: 44
	[CallbackIdentity(1021)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct AppProofOfPurchaseKeyResponse_t
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000839 RID: 2105 RVA: 0x0000BCB5 File Offset: 0x00009EB5
		// (set) Token: 0x0600083A RID: 2106 RVA: 0x0000BCC2 File Offset: 0x00009EC2
		public string m_rgchKey
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchKey_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchKey_, 240);
			}
		}

		// Token: 0x0400000F RID: 15
		public const int k_iCallback = 1021;

		// Token: 0x04000010 RID: 16
		public EResult m_eResult;

		// Token: 0x04000011 RID: 17
		public uint m_nAppID;

		// Token: 0x04000012 RID: 18
		public uint m_cchKeyLength;

		// Token: 0x04000013 RID: 19
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 240)]
		private byte[] m_rgchKey_;
	}
}
