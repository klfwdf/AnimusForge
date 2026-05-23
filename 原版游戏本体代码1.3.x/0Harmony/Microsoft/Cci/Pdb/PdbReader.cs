using System;
using System.IO;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000440 RID: 1088
	internal class PdbReader
	{
		// Token: 0x060017B1 RID: 6065 RVA: 0x0004A963 File Offset: 0x00048B63
		internal PdbReader(Stream reader, int pageSize)
		{
			this.pageSize = pageSize;
			this.reader = reader;
		}

		// Token: 0x060017B2 RID: 6066 RVA: 0x0004A979 File Offset: 0x00048B79
		internal void Seek(int page, int offset)
		{
			this.reader.Seek((long)(page * this.pageSize + offset), SeekOrigin.Begin);
		}

		// Token: 0x060017B3 RID: 6067 RVA: 0x0004A993 File Offset: 0x00048B93
		internal void Read(byte[] bytes, int offset, int count)
		{
			this.reader.Read(bytes, offset, count);
		}

		// Token: 0x060017B4 RID: 6068 RVA: 0x0004A9A4 File Offset: 0x00048BA4
		internal int PagesFromSize(int size)
		{
			return (size + this.pageSize - 1) / this.pageSize;
		}

		// Token: 0x04001033 RID: 4147
		internal readonly int pageSize;

		// Token: 0x04001034 RID: 4148
		internal readonly Stream reader;
	}
}
