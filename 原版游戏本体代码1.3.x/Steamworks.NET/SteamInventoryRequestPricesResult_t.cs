using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000074 RID: 116
	[CallbackIdentity(4705)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInventoryRequestPricesResult_t
	{
		// Token: 0x17000008 RID: 8
		// (get) Token: 0x06000847 RID: 2119 RVA: 0x0000BD8F File Offset: 0x00009F8F
		// (set) Token: 0x06000848 RID: 2120 RVA: 0x0000BD9C File Offset: 0x00009F9C
		public string m_rgchCurrency
		{
			get
			{
				return InteropHelp.ByteArrayToStringUTF8(this.m_rgchCurrency_);
			}
			set
			{
				InteropHelp.StringToByteArrayUTF8(value, this.m_rgchCurrency_, 4);
			}
		}

		// Token: 0x04000126 RID: 294
		public const int k_iCallback = 4705;

		// Token: 0x04000127 RID: 295
		public EResult m_result;

		// Token: 0x04000128 RID: 296
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		private byte[] m_rgchCurrency_;
	}
}
