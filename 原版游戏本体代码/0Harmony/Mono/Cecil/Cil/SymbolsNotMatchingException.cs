using System;
using System.Runtime.Serialization;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000322 RID: 802
	[Serializable]
	internal sealed class SymbolsNotMatchingException : InvalidOperationException
	{
		// Token: 0x060014C6 RID: 5318 RVA: 0x000418A9 File Offset: 0x0003FAA9
		public SymbolsNotMatchingException(string message)
			: base(message)
		{
		}

		// Token: 0x060014C7 RID: 5319 RVA: 0x000418B2 File Offset: 0x0003FAB2
		private SymbolsNotMatchingException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
