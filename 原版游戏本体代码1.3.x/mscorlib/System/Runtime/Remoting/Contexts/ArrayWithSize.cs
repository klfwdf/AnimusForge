using System;

namespace System.Runtime.Remoting.Contexts
{
	// Token: 0x02000811 RID: 2065
	internal class ArrayWithSize
	{
		// Token: 0x060058DA RID: 22746 RVA: 0x00139372 File Offset: 0x00137572
		internal ArrayWithSize(IDynamicMessageSink[] sinks, int count)
		{
			this.Sinks = sinks;
			this.Count = count;
		}

		// Token: 0x0400287A RID: 10362
		internal IDynamicMessageSink[] Sinks;

		// Token: 0x0400287B RID: 10363
		internal int Count;
	}
}
