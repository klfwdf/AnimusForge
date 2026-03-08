using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MCM.Common;

namespace MCM.UI.Exceptions
{
	// Token: 0x0200002A RID: 42
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	public class MCMUIEmbedResourceNotFoundException : MCMException
	{
		// Token: 0x06000188 RID: 392 RVA: 0x0000734C File Offset: 0x0000554C
		public MCMUIEmbedResourceNotFoundException()
		{
		}

		// Token: 0x06000189 RID: 393 RVA: 0x00007354 File Offset: 0x00005554
		public MCMUIEmbedResourceNotFoundException(string message)
			: base(message)
		{
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000735D File Offset: 0x0000555D
		public MCMUIEmbedResourceNotFoundException(string message, Exception inner)
			: base(message, inner)
		{
		}

		// Token: 0x0600018B RID: 395 RVA: 0x00007367 File Offset: 0x00005567
		protected MCMUIEmbedResourceNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
