using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200006C RID: 108
	[CallbackIdentity(2801)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct SteamInputDeviceConnected_t
	{
		// Token: 0x0400010B RID: 267
		public const int k_iCallback = 2801;

		// Token: 0x0400010C RID: 268
		public InputHandle_t m_ulConnectedDeviceHandle;
	}
}
