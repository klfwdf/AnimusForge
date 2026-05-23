using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000046 RID: 70
	[CallbackIdentity(202)]
	[StructLayout(LayoutKind.Sequential, Pack = 4)]
	public struct GSClientDeny_t
	{
		// Token: 0x17000006 RID: 6
		// (get) Token: 0x06000843 RID: 2115 RVA: 0x0000BD4F File Offset: 0x00009F4F
		// (set) Token: 0x06000844 RID: 2116 RVA: 0x0000BD5C File Offset: 0x00009F5C
		public string m_rgchOptionalText
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchOptionalText_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchOptionalText_, 128);
			}
		}

		// Token: 0x04000066 RID: 102
		public const int k_iCallback = 202;

		// Token: 0x04000067 RID: 103
		public CSteamID m_SteamID;

		// Token: 0x04000068 RID: 104
		public EDenyReason m_eDenyReason;

		// Token: 0x04000069 RID: 105
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
		private byte[] m_rgchOptionalText_;
	}
}
