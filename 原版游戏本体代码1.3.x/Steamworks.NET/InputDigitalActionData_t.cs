using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000167 RID: 359
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct InputDigitalActionData_t
	{
		// Token: 0x04000980 RID: 2432
		public byte bState;

		// Token: 0x04000981 RID: 2433
		public byte bActive;
	}
}
