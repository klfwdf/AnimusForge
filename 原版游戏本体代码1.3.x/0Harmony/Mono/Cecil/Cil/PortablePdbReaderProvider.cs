using System;
using System.IO;
using Mono.Cecil.PE;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002FC RID: 764
	internal sealed class PortablePdbReaderProvider : ISymbolReaderProvider
	{
		// Token: 0x060013E3 RID: 5091 RVA: 0x0003FCF4 File Offset: 0x0003DEF4
		public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			Mixin.CheckFileName(fileName);
			FileStream file = File.OpenRead(Mixin.GetPdbFileName(fileName));
			return this.GetSymbolReader(module, Disposable.Owned<Stream>(file), file.Name);
		}

		// Token: 0x060013E4 RID: 5092 RVA: 0x0003FD2C File Offset: 0x0003DF2C
		public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream)
		{
			Mixin.CheckModule(module);
			Mixin.CheckStream(symbolStream);
			return this.GetSymbolReader(module, Disposable.NotOwned<Stream>(symbolStream), symbolStream.GetFileName());
		}

		// Token: 0x060013E5 RID: 5093 RVA: 0x0003FD50 File Offset: 0x0003DF50
		private ISymbolReader GetSymbolReader(ModuleDefinition module, Disposable<Stream> symbolStream, string fileName)
		{
			uint num;
			return new PortablePdbReader(ImageReader.ReadPortablePdb(symbolStream, fileName, out num), module);
		}
	}
}
