using System;
using System.IO;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000320 RID: 800
	internal interface ISymbolReaderProvider
	{
		// Token: 0x060014C2 RID: 5314
		ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName);

		// Token: 0x060014C3 RID: 5315
		ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream);
	}
}
