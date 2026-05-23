using System;
using System.IO;
using Mono.Cecil.Cil;

namespace Mono.Cecil.Pdb
{
	// Token: 0x02000367 RID: 871
	internal sealed class NativePdbWriterProvider : ISymbolWriterProvider
	{
		// Token: 0x06001728 RID: 5928 RVA: 0x000477F6 File Offset: 0x000459F6
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			Mixin.CheckFileName(fileName);
			return new NativePdbWriter(module, NativePdbWriterProvider.CreateWriter(module, Mixin.GetPdbFileName(fileName)));
		}

		// Token: 0x06001729 RID: 5929 RVA: 0x00047816 File Offset: 0x00045A16
		private static SymWriter CreateWriter(ModuleDefinition module, string pdb)
		{
			SymWriter symWriter = new SymWriter();
			if (File.Exists(pdb))
			{
				File.Delete(pdb);
			}
			symWriter.Initialize(new ModuleMetadata(module), pdb, true);
			return symWriter;
		}

		// Token: 0x0600172A RID: 5930 RVA: 0x00045AA1 File Offset: 0x00043CA1
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
		{
			throw new NotImplementedException();
		}
	}
}
