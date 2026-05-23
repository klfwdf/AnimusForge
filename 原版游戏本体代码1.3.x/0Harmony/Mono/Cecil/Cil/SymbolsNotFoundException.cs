using System;
using System.IO;
using System.Runtime.Serialization;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000321 RID: 801
	[Serializable]
	internal sealed class SymbolsNotFoundException : FileNotFoundException
	{
		// Token: 0x060014C4 RID: 5316 RVA: 0x000418A0 File Offset: 0x0003FAA0
		public SymbolsNotFoundException(string message)
			: base(message)
		{
		}

		// Token: 0x060014C5 RID: 5317 RVA: 0x00029932 File Offset: 0x00027B32
		private SymbolsNotFoundException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}
	}
}
