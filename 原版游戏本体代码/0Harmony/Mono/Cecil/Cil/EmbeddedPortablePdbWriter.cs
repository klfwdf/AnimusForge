using System;
using System.IO;
using System.IO.Compression;
using Mono.Cecil.PE;

namespace Mono.Cecil.Cil
{
	// Token: 0x02000303 RID: 771
	internal sealed class EmbeddedPortablePdbWriter : ISymbolWriter, IDisposable
	{
		// Token: 0x06001415 RID: 5141 RVA: 0x000406D0 File Offset: 0x0003E8D0
		internal EmbeddedPortablePdbWriter(Stream stream, PortablePdbWriter writer)
		{
			this.stream = stream;
			this.writer = writer;
		}

		// Token: 0x06001416 RID: 5142 RVA: 0x000406E6 File Offset: 0x0003E8E6
		public ISymbolReaderProvider GetReaderProvider()
		{
			return new EmbeddedPortablePdbReaderProvider();
		}

		// Token: 0x06001417 RID: 5143 RVA: 0x000406ED File Offset: 0x0003E8ED
		public void Write(MethodDebugInformation info)
		{
			this.writer.Write(info);
		}

		// Token: 0x06001418 RID: 5144 RVA: 0x000406FB File Offset: 0x0003E8FB
		public void Write(ICustomDebugInformationProvider provider)
		{
			this.writer.Write(provider);
		}

		// Token: 0x06001419 RID: 5145 RVA: 0x0004070C File Offset: 0x0003E90C
		public ImageDebugHeader GetDebugHeader()
		{
			ImageDebugHeader pdbDebugHeader = this.writer.GetDebugHeader();
			ImageDebugDirectory directory = new ImageDebugDirectory
			{
				Type = ImageDebugType.EmbeddedPortablePdb,
				MajorVersion = 256,
				MinorVersion = 256
			};
			MemoryStream data = new MemoryStream();
			BinaryStreamWriter binaryStreamWriter = new BinaryStreamWriter(data);
			binaryStreamWriter.WriteByte(77);
			binaryStreamWriter.WriteByte(80);
			binaryStreamWriter.WriteByte(68);
			binaryStreamWriter.WriteByte(66);
			binaryStreamWriter.WriteInt32((int)this.stream.Length);
			this.stream.Position = 0L;
			using (DeflateStream compress_stream = new DeflateStream(data, CompressionMode.Compress, true))
			{
				this.stream.CopyTo(compress_stream);
			}
			directory.SizeOfData = (int)data.Length;
			ImageDebugHeaderEntry[] debugHeaderEntries = new ImageDebugHeaderEntry[pdbDebugHeader.Entries.Length + 1];
			for (int i = 0; i < pdbDebugHeader.Entries.Length; i++)
			{
				debugHeaderEntries[i] = pdbDebugHeader.Entries[i];
			}
			debugHeaderEntries[debugHeaderEntries.Length - 1] = new ImageDebugHeaderEntry(directory, data.ToArray());
			return new ImageDebugHeader(debugHeaderEntries);
		}

		// Token: 0x0600141A RID: 5146 RVA: 0x0004082C File Offset: 0x0003EA2C
		public void Write()
		{
			this.writer.Write();
		}

		// Token: 0x0600141B RID: 5147 RVA: 0x00040839 File Offset: 0x0003EA39
		public void Dispose()
		{
			this.writer.Dispose();
		}

		// Token: 0x040009F8 RID: 2552
		private readonly Stream stream;

		// Token: 0x040009F9 RID: 2553
		private readonly PortablePdbWriter writer;
	}
}
