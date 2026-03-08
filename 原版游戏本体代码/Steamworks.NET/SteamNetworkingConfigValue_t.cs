using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000198 RID: 408
	[Serializable]
	public struct SteamNetworkingConfigValue_t
	{
		// Token: 0x04000A31 RID: 2609
		public ESteamNetworkingConfigValue m_eValue;

		// Token: 0x04000A32 RID: 2610
		public ESteamNetworkingConfigDataType m_eDataType;

		// Token: 0x04000A33 RID: 2611
		public SteamNetworkingConfigValue_t.OptionValue m_val;

		// Token: 0x020001CA RID: 458
		[StructLayout(LayoutKind.Explicit)]
		public struct OptionValue
		{
			// Token: 0x04000ACE RID: 2766
			[FieldOffset(0)]
			public int m_int32;

			// Token: 0x04000ACF RID: 2767
			[FieldOffset(0)]
			public long m_int64;

			// Token: 0x04000AD0 RID: 2768
			[FieldOffset(0)]
			public float m_float;

			// Token: 0x04000AD1 RID: 2769
			[FieldOffset(0)]
			public IntPtr m_string;

			// Token: 0x04000AD2 RID: 2770
			[FieldOffset(0)]
			public IntPtr m_functionPtr;
		}
	}
}
