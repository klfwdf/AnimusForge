using System;

namespace TaleWorlds.Library
{
	// Token: 0x0200000E RID: 14
	public abstract class AsyncRunner
	{
		// Token: 0x06000031 RID: 49
		public abstract void Run();

		// Token: 0x06000032 RID: 50
		public abstract void SyncTick();

		// Token: 0x06000033 RID: 51
		public abstract void OnRemove();
	}
}
