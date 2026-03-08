using System;

namespace System.Reflection
{
	// Token: 0x020005F2 RID: 1522
	internal sealed class LoaderAllocator
	{
		// Token: 0x06004674 RID: 18036 RVA: 0x001027B0 File Offset: 0x001009B0
		private LoaderAllocator()
		{
			this.m_slots = new object[5];
			this.m_scout = new LoaderAllocatorScout();
		}

		// Token: 0x04001CD5 RID: 7381
		private LoaderAllocatorScout m_scout;

		// Token: 0x04001CD6 RID: 7382
		private object[] m_slots;

		// Token: 0x04001CD7 RID: 7383
		internal CerHashtable<RuntimeMethodInfo, RuntimeMethodInfo> m_methodInstantiations;

		// Token: 0x04001CD8 RID: 7384
		private int m_slotsUsed;
	}
}
