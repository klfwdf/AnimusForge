using System;
using System.Threading;

namespace TaleWorlds.Library
{
	// Token: 0x0200008F RID: 143
	public static class SingleThreadedSynchronizationContextManager
	{
		// Token: 0x0600050D RID: 1293 RVA: 0x00012564 File Offset: 0x00010764
		public static void Initialize()
		{
			if (SingleThreadedSynchronizationContextManager._synchronizationContext == null)
			{
				SingleThreadedSynchronizationContextManager._synchronizationContext = new SingleThreadedSynchronizationContext();
				SynchronizationContext.SetSynchronizationContext(SingleThreadedSynchronizationContextManager._synchronizationContext);
			}
		}

		// Token: 0x0600050E RID: 1294 RVA: 0x00012581 File Offset: 0x00010781
		public static void Tick()
		{
			SingleThreadedSynchronizationContextManager._synchronizationContext.Tick();
		}

		// Token: 0x04000195 RID: 405
		private static SingleThreadedSynchronizationContext _synchronizationContext;
	}
}
