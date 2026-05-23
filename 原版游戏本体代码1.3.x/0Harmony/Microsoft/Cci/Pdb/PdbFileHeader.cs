using System;
using System.IO;
using System.Linq;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000437 RID: 1079
	internal class PdbFileHeader
	{
		// Token: 0x06001796 RID: 6038 RVA: 0x00049D90 File Offset: 0x00047F90
		internal PdbFileHeader(Stream reader, BitAccess bits)
		{
			bits.MinCapacity(56);
			reader.Seek(0L, SeekOrigin.Begin);
			bits.FillBuffer(reader, 52);
			this.magic = new byte[32];
			bits.ReadBytes(this.magic);
			bits.ReadInt32(out this.pageSize);
			bits.ReadInt32(out this.freePageMap);
			bits.ReadInt32(out this.pagesUsed);
			bits.ReadInt32(out this.directorySize);
			bits.ReadInt32(out this.zero);
			if (!this.magic.SequenceEqual(this.windowsPdbMagic))
			{
				throw new PdbException("The PDB file is not recognized as a Windows PDB file", new object[0]);
			}
			int directoryPages = ((this.directorySize + this.pageSize - 1) / this.pageSize * 4 + this.pageSize - 1) / this.pageSize;
			this.directoryRoot = new int[directoryPages];
			bits.FillBuffer(reader, directoryPages * 4);
			bits.ReadInt32(this.directoryRoot);
		}

		// Token: 0x04001004 RID: 4100
		private readonly byte[] windowsPdbMagic = new byte[]
		{
			77, 105, 99, 114, 111, 115, 111, 102, 116, 32,
			67, 47, 67, 43, 43, 32, 77, 83, 70, 32,
			55, 46, 48, 48, 13, 10, 26, 68, 83, 0,
			0, 0
		};

		// Token: 0x04001005 RID: 4101
		internal readonly byte[] magic;

		// Token: 0x04001006 RID: 4102
		internal readonly int pageSize;

		// Token: 0x04001007 RID: 4103
		internal int freePageMap;

		// Token: 0x04001008 RID: 4104
		internal int pagesUsed;

		// Token: 0x04001009 RID: 4105
		internal int directorySize;

		// Token: 0x0400100A RID: 4106
		internal readonly int zero;

		// Token: 0x0400100B RID: 4107
		internal int[] directoryRoot;
	}
}
