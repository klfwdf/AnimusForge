using System;
using System.IO;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000327 RID: 807
	internal interface ISymbolWriterProvider
	{
		// Token: 0x060014D6 RID: 5334
		ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName);

		// Token: 0x060014D7 RID: 5335
		ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream);
	}
}
