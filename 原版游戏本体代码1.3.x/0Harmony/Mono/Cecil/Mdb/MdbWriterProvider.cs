using System;
using System.IO;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Mdb
{
	// Token: 0x02000352 RID: 850
	internal sealed class MdbWriterProvider : ISymbolWriterProvider
	{
		// Token: 0x060015D6 RID: 5590 RVA: 0x00045A8C File Offset: 0x00043C8C
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			Mixin.CheckFileName(fileName);
			return new MdbWriter(module, fileName);
		}

		// Token: 0x060015D7 RID: 5591 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
		{
			throw new NotImplementedException();
		}
	}
}
