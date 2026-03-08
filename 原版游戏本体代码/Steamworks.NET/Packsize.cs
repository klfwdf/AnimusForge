using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x020001BE RID: 446
	public static class Packsize
	{
		// Token: 0x06000B12 RID: 2834 RVA: 0x0000F680 File Offset: 0x0000D880
		public static bool Test()
		{
			int num = Marshal.SizeOf(typeof(Packsize.ValvePackingSentinel_t));
			int num2 = Marshal.SizeOf(typeof(RemoteStorageEnumerateUserSubscribedFilesResult_t));
			return num == 32 && num2 == 616;
		}

		// Token: 0x04000A93 RID: 2707
		public const int value = 8;

		// Token: 0x020001E9 RID: 489
		[StructLayout(LayoutKind.Sequential, Pack = 8)]
		private struct ValvePackingSentinel_t
		{
			// Token: 0x04000AE1 RID: 2785
			private uint m_u32;

			// Token: 0x04000AE2 RID: 2786
			private ulong m_u64;

			// Token: 0x04000AE3 RID: 2787
			private ushort m_u16;

			// Token: 0x04000AE4 RID: 2788
			private double m_d;
		}
	}
}
