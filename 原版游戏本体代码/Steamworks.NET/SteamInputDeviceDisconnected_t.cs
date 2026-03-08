using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200006D RID: 109
	[CallbackIdentity(2802)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInputDeviceDisconnected_t
	{
		// Token: 0x0400010D RID: 269
		public const int k_iCallback = 2802;

		// Token: 0x0400010E RID: 270
		public InputHandle_t m_ulDisconnectedDeviceHandle;
	}
}
