using System;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x020004ED RID: 1261
	[FriendAccessAllowed]
	[SecurityCritical]
	internal class WinRTSynchronizationContextFactoryBase
	{
		// Token: 0x06003B90 RID: 15248 RVA: 0x000E2600 File Offset: 0x000E0800
		[SecurityCritical]
		public virtual SynchronizationContext Create(object coreDispatcher)
		{
			return null;
		}
	}
}
