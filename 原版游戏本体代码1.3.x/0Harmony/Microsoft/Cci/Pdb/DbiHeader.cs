using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000429 RID: 1065
	internal struct DbiHeader
	{
		// Token: 0x0600176E RID: 5998 RVA: 0x000484A0 File Offset: 0x000466A0
		internal DbiHeader(BitAccess bits)
		{
			bits.ReadInt32(out this.sig);
			bits.ReadInt32(out this.ver);
			bits.ReadInt32(out this.age);
			bits.ReadInt16(out this.gssymStream);
			bits.ReadUInt16(out this.vers);
			bits.ReadInt16(out this.pssymStream);
			bits.ReadUInt16(out this.pdbver);
			bits.ReadInt16(out this.symrecStream);
			bits.ReadUInt16(out this.pdbver2);
			bits.ReadInt32(out this.gpmodiSize);
			bits.ReadInt32(out this.secconSize);
			bits.ReadInt32(out this.secmapSize);
			bits.ReadInt32(out this.filinfSize);
			bits.ReadInt32(out this.tsmapSize);
			bits.ReadInt32(out this.mfcIndex);
			bits.ReadInt32(out this.dbghdrSize);
			bits.ReadInt32(out this.ecinfoSize);
			bits.ReadUInt16(out this.flags);
			bits.ReadUInt16(out this.machine);
			bits.ReadInt32(out this.reserved);
		}

		// Token: 0x04000FCA RID: 4042
		internal int sig;

		// Token: 0x04000FCB RID: 4043
		internal int ver;

		// Token: 0x04000FCC RID: 4044
		internal int age;

		// Token: 0x04000FCD RID: 4045
		internal short gssymStream;

		// Token: 0x04000FCE RID: 4046
		internal ushort vers;

		// Token: 0x04000FCF RID: 4047
		internal short pssymStream;

		// Token: 0x04000FD0 RID: 4048
		internal ushort pdbver;

		// Token: 0x04000FD1 RID: 4049
		internal short symrecStream;

		// Token: 0x04000FD2 RID: 4050
		internal ushort pdbver2;

		// Token: 0x04000FD3 RID: 4051
		internal int gpmodiSize;

		// Token: 0x04000FD4 RID: 4052
		internal int secconSize;

		// Token: 0x04000FD5 RID: 4053
		internal int secmapSize;

		// Token: 0x04000FD6 RID: 4054
		internal int filinfSize;

		// Token: 0x04000FD7 RID: 4055
		internal int tsmapSize;

		// Token: 0x04000FD8 RID: 4056
		internal int mfcIndex;

		// Token: 0x04000FD9 RID: 4057
		internal int dbghdrSize;

		// Token: 0x04000FDA RID: 4058
		internal int ecinfoSize;

		// Token: 0x04000FDB RID: 4059
		internal ushort flags;

		// Token: 0x04000FDC RID: 4060
		internal ushort machine;

		// Token: 0x04000FDD RID: 4061
		internal int reserved;
	}
}
