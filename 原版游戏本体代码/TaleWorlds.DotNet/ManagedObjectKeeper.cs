using System;
using System.Runtime.InteropServices;

namespace TaleWorlds.DotNet
{
	// Token: 0x02000024 RID: 36
	internal class ManagedObjectKeeper
	{
		// Token: 0x04000051 RID: 81
		public int TimerToReleaseStrongRef;

		// Token: 0x04000052 RID: 82
		public GCHandle gcHandle;
	}
}
