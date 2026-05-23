using System;
using System.IO;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000328 RID: 808
	internal class DefaultSymbolWriterProvider : ISymbolWriterProvider
	{
		// Token: 0x060014D8 RID: 5336 RVA: 0x00041CA8 File Offset: 0x0003FEA8
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
		{
			ISymbolReader reader = module.SymbolReader;
			if (reader == null)
			{
				throw new InvalidOperationException();
			}
			if (module.Image != null && module.Image.HasDebugTables())
			{
				return null;
			}
			return reader.GetWriterProvider().GetSymbolWriter(module, fileName);
		}

		// Token: 0x060014D9 RID: 5337 RVA: 0x00003BBE File Offset: 0x00001DBE
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
		{
			throw new NotSupportedException();
		}
	}
}
