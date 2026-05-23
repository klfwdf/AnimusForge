using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020000BE RID: 190
	[CallbackIdentity(1331)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct RemoteStorageFileWriteAsyncComplete_t
	{
		// Token: 0x04000238 RID: 568
		public const int k_iCallback = 1331;

		// Token: 0x04000239 RID: 569
		public EResult m_eResult;
	}
}
