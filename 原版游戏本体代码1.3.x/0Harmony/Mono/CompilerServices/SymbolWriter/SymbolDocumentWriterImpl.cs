using System;
using System.Diagnostics.SymbolStore;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200034B RID: 843
	internal class SymbolDocumentWriterImpl : ISymbolDocumentWriter, ISourceFile, ICompileUnit
	{
		// Token: 0x060015B7 RID: 5559 RVA: 0x000455D2 File Offset: 0x000437D2
		public SymbolDocumentWriterImpl(CompileUnitEntry comp_unit)
		{
			this.comp_unit = comp_unit;
		}

		// Token: 0x060015B8 RID: 5560 RVA: 0x0001B842 File Offset: 0x00019A42
		public void SetCheckSum(Guid algorithmId, byte[] checkSum)
		{
		}

		// Token: 0x060015B9 RID: 5561 RVA: 0x0001B842 File Offset: 0x00019A42
		public void SetSource(byte[] source)
		{
		}

		// Token: 0x1700057B RID: 1403
		// (get) Token: 0x060015BA RID: 5562 RVA: 0x000455E1 File Offset: 0x000437E1
		SourceFileEntry ISourceFile.Entry
		{
			get
			{
				return this.comp_unit.SourceFile;
			}
		}

		// Token: 0x1700057C RID: 1404
		// (get) Token: 0x060015BB RID: 5563 RVA: 0x000455EE File Offset: 0x000437EE
		public CompileUnitEntry Entry
		{
			get
			{
				return this.comp_unit;
			}
		}

		// Token: 0x04000B1C RID: 2844
		private CompileUnitEntry comp_unit;
	}
}
