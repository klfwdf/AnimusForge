using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x0200002D RID: 45
	[CallbackIdentity(1023)]
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct FileDetailsResult_t
	{
		// Token: 0x04000014 RID: 20
		public const int k_iCallback = 1023;

		// Token: 0x04000015 RID: 21
		public EResult m_eResult;

		// Token: 0x04000016 RID: 22
		public ulong m_ulFileSize;

		// Token: 0x04000017 RID: 23
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
		public byte[] m_FileSHA;

		// Token: 0x04000018 RID: 24
		public uint m_unFlags;
	}
}
