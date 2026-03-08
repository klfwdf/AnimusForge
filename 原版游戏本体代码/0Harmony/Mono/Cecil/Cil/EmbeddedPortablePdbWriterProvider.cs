using System;
using System.IO;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000302 RID: 770
	internal sealed class EmbeddedPortablePdbWriterProvider : ISymbolWriterProvider
	{
		// Token: 0x06001412 RID: 5138 RVA: 0x00040698 File Offset: 0x0003E898
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			Mixin.CheckFileName(fileName);
			MemoryStream stream = new MemoryStream();
			PortablePdbWriter pdb_writer = (PortablePdbWriter)new PortablePdbWriterProvider().GetSymbolWriter(module, stream);
			return new EmbeddedPortablePdbWriter(stream, pdb_writer);
		}

		// Token: 0x06001413 RID: 5139 RVA: 0x00003BBE File Offset: 0x00001DBE
		public ISymbolWriter GetSymbolWriter(ModuleDefinition module, Stream symbolStream)
		{
			throw new NotSupportedException();
		}
	}
}
