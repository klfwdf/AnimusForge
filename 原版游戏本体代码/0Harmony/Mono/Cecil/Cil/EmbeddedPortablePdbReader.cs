using System;
using Mono.Collections.Generic;

namespace Mono.Cecil.Cil
{
	// Token: 0x020002FF RID: 767
	internal sealed class EmbeddedPortablePdbReader : ISymbolReader, IDisposable
	{
		// Token: 0x060013F9 RID: 5113 RVA: 0x00040030 File Offset: 0x0003E230
		internal EmbeddedPortablePdbReader(PortablePdbReader reader)
		{
			if (reader == null)
			{
				throw new ArgumentNullException();
			}
			this.reader = reader;
		}

		// Token: 0x060013FA RID: 5114 RVA: 0x00040048 File Offset: 0x0003E248
		public ISymbolWriterProvider GetWriterProvider()
		{
			return new EmbeddedPortablePdbWriterProvider();
		}

		// Token: 0x060013FB RID: 5115 RVA: 0x0004004F File Offset: 0x0003E24F
		public bool ProcessDebugHeader(ImageDebugHeader header)
		{
			return this.reader.ProcessDebugHeader(header);
		}

		// Token: 0x060013FC RID: 5116 RVA: 0x0004005D File Offset: 0x0003E25D
		public MethodDebugInformation Read(MethodDefinition method)
		{
			return this.reader.Read(method);
		}

		// Token: 0x060013FD RID: 5117 RVA: 0x0004006B File Offset: 0x0003E26B
		public Collection<CustomDebugInformation> Read(ICustomDebugInformationProvider provider)
		{
			return this.reader.Read(provider);
		}

		// Token: 0x060013FE RID: 5118 RVA: 0x00040079 File Offset: 0x0003E279
		public void Dispose()
		{
			this.reader.Dispose();
		}

		// Token: 0x040009EF RID: 2543
		private readonly PortablePdbReader reader;
	}
}
