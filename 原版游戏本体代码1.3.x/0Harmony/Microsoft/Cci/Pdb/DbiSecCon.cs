using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200042B RID: 1067
	internal struct DbiSecCon
	{
		// Token: 0x06001770 RID: 6000 RVA: 0x0004867C File Offset: 0x0004687C
		internal DbiSecCon(BitAccess bits)
		{
			bits.ReadInt16(out this.section);
			bits.ReadInt16(out this.pad1);
			bits.ReadInt32(out this.offset);
			bits.ReadInt32(out this.size);
			bits.ReadUInt32(out this.flags);
			bits.ReadInt16(out this.module);
			bits.ReadInt16(out this.pad2);
			bits.ReadUInt32(out this.dataCrc);
			bits.ReadUInt32(out this.relocCrc);
		}

		// Token: 0x04000FEB RID: 4075
		internal short section;

		// Token: 0x04000FEC RID: 4076
		internal short pad1;

		// Token: 0x04000FED RID: 4077
		internal int offset;

		// Token: 0x04000FEE RID: 4078
		internal int size;

		// Token: 0x04000FEF RID: 4079
		internal uint flags;

		// Token: 0x04000FF0 RID: 4080
		internal short module;

		// Token: 0x04000FF1 RID: 4081
		internal short pad2;

		// Token: 0x04000FF2 RID: 4082
		internal uint dataCrc;

		// Token: 0x04000FF3 RID: 4083
		internal uint relocCrc;
	}
}
