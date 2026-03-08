using System;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace MonoMod.Utils
{
	// Token: 0x020008E6 RID: 2278
	[NullableContext(1)]
	[Nullable(0)]
	[Serializable]
	internal class RelinkTargetNotFoundException : RelinkFailedException
	{
		// Token: 0x06002F78 RID: 12152 RVA: 0x000A5358 File Offset: 0x000A3558
		public RelinkTargetNotFoundException(IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: base(RelinkFailedException.Format("MonoMod relinker failed finding", mtp, context), mtp, context)
		{
		}

		// Token: 0x06002F79 RID: 12153 RVA: 0x000A536E File Offset: 0x000A356E
		public RelinkTargetNotFoundException(string message, IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: base(message ?? "MonoMod relinker failed finding", mtp, context)
		{
		}

		// Token: 0x06002F7A RID: 12154 RVA: 0x000A5382 File Offset: 0x000A3582
		public RelinkTargetNotFoundException(string message, Exception innerException, IMetadataTokenProvider mtp, [Nullable(2)] IMetadataTokenProvider context = null)
			: base(message ?? "MonoMod relinker failed finding", innerException, mtp, context)
		{
		}

		// Token: 0x04003BAC RID: 15276
		public new const string DefaultMessage = "MonoMod relinker failed finding";
	}
}
