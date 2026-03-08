using System;
using System.IO;
using System.IO.Compression;
using Mono.Cecil.PE;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002FE RID: 766
	internal sealed class EmbeddedPortablePdbReaderProvider : ISymbolReaderProvider
	{
		// Token: 0x060013F5 RID: 5109 RVA: 0x0003FF90 File Offset: 0x0003E190
		public ISymbolReader GetSymbolReader(ModuleDefinition module, string fileName)
		{
			Mixin.CheckModule(module);
			ImageDebugHeaderEntry entry = module.GetDebugHeader().GetEmbeddedPortablePdbEntry();
			if (entry == null)
			{
				throw new InvalidOperationException();
			}
			return new EmbeddedPortablePdbReader((PortablePdbReader)new PortablePdbReaderProvider().GetSymbolReader(module, EmbeddedPortablePdbReaderProvider.GetPortablePdbStream(entry)));
		}

		// Token: 0x060013F6 RID: 5110 RVA: 0x0003FFD4 File Offset: 0x0003E1D4
		private static Stream GetPortablePdbStream(ImageDebugHeaderEntry entry)
		{
			MemoryStream stream = new MemoryStream(entry.Data);
			BinaryStreamReader binaryStreamReader = new BinaryStreamReader(stream);
			binaryStreamReader.ReadInt32();
			MemoryStream decompressed_stream = new MemoryStream(binaryStreamReader.ReadInt32());
			using (DeflateStream deflate_stream = new DeflateStream(stream, CompressionMode.Decompress, true))
			{
				deflate_stream.CopyTo(decompressed_stream);
			}
			return decompressed_stream;
		}

		// Token: 0x060013F7 RID: 5111 RVA: 0x00003BBE File Offset: 0x00001DBE
		public ISymbolReader GetSymbolReader(ModuleDefinition module, Stream symbolStream)
		{
			throw new NotSupportedException();
		}
	}
}
