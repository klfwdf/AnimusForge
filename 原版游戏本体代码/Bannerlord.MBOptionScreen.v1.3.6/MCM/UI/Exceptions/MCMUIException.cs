using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MCM.Common;

namespace MCM.UI.Exceptions
{
	// Token: 0x0200002B RID: 43
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	public class MCMUIException : MCMException
	{
		// Token: 0x0600018C RID: 396 RVA: 0x00007371 File Offset: 0x00005571
		public MCMUIException()
		{
		}

		// Token: 0x0600018D RID: 397 RVA: 0x00007379 File Offset: 0x00005579
		public MCMUIException(string message)
			: base(message)
		{
		}

		// Token: 0x0600018E RID: 398 RVA: 0x00007382 File Offset: 0x00005582
		public MCMUIException(string message, Exception inner)
			: base(message, inner)
		{
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000738C File Offset: 0x0000558C
		protected MCMUIException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
