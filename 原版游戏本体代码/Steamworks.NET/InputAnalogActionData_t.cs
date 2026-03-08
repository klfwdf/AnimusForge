using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000166 RID: 358
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct InputAnalogActionData_t
	{
		// Token: 0x0400097C RID: 2428
		public EInputSourceMode eMode;

		// Token: 0x0400097D RID: 2429
		public float x;

		// Token: 0x0400097E RID: 2430
		public float y;

		// Token: 0x0400097F RID: 2431
		public byte bActive;
	}
}
