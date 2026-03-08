using System;
using System.IO;
using Mono.Cecil.PE;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000300 RID: 768
	internal sealed class PortablePdbWriterProvider : ISymbolWriterProvider
	{
		// Token: 0x060013FF RID: 5119 RVA: 0x00040088 File Offset: 0x0003E288
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			Mixin.CheckFileName(fileName);
			FileStream file = File.Open(Mixin.GetPdbFileName(fileName), FileMode.OpenOrCreate, FileAccess.ReadWrite);
			return this.GetSymbolWriter(module, Disposable.Owned<Stream>(file), Disposable.NotOwned<Stream>(null));
		}

		// Token: 0x06001400 RID: 5120 RVA: 0x000400C2 File Offset: 0x0003E2C2
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
		{
			Mixin.CheckModule(module);
			Mixin.CheckStream(symbolStream);
			return this.GetSymbolWriter(module, Disposable.Owned<Stream>(new MemoryStream()), Disposable.NotOwned<Stream>(symbolStream));
		}

		// Token: 0x06001401 RID: 5121 RVA: 0x000400E8 File Offset: 0x0003E2E8
		private ISymbolWriter GetSymbolWriter(ModuleDefinition module, Disposable<Stream> stream, Disposable<Stream> final_stream)
		{
			MetadataBuilder metadata = new MetadataBuilder(module, this);
			ImageWriter writer = ImageWriter.CreateDebugWriter(module, metadata, stream);
			return new PortablePdbWriter(metadata, module, writer, final_stream);
		}
	}
}
