using System;
using System.Runtime.InteropServices;

namespace Steamworks
{
	// Token: 0x02000168 RID: 360
	[StructLayout(LayoutKind.Sequential, Pack = 8)]
	public struct InputMotionData_t
	{
		// Token: 0x04000982 RID: 2434
		public float rotQuatX;

		// Token: 0x04000983 RID: 2435
		public float rotQuatY;

		// Token: 0x04000984 RID: 2436
		public float rotQuatZ;

		// Token: 0x04000985 RID: 2437
		public float rotQuatW;

		// Token: 0x04000986 RID: 2438
		public float posAccelX;

		// Token: 0x04000987 RID: 2439
		public float posAccelY;

		// Token: 0x04000988 RID: 2440
		public float posAccelZ;

		// Token: 0x04000989 RID: 2441
		public float rotVelX;

		// Token: 0x0400098A RID: 2442
		public float rotVelY;

		// Token: 0x0400098B RID: 2443
		public float rotVelZ;
	}
}
