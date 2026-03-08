using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000F1 RID: 241
	[CallbackIdentity(702)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct LowBatteryPower_t
	{
		// Token: 0x040002EF RID: 751
		public const int k_iCallback = 702;

		// Token: 0x040002F0 RID: 752
		public byte m_nMinutesBatteryLeft;
	}
}
