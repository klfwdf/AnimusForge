using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000428 RID: 1064
	internal struct DbiDbgHdr
	{
		// Token: 0x0600176D RID: 5997 RVA: 0x0004840C File Offset: 0x0004660C
		internal DbiDbgHdr(BitAccess bits)
		{
			bits.ReadUInt16(out this.snFPO);
			bits.ReadUInt16(out this.snException);
			bits.ReadUInt16(out this.snFixup);
			bits.ReadUInt16(out this.snOmapToSrc);
			bits.ReadUInt16(out this.snOmapFromSrc);
			bits.ReadUInt16(out this.snSectionHdr);
			bits.ReadUInt16(out this.snTokenRidMap);
			bits.ReadUInt16(out this.snXdata);
			bits.ReadUInt16(out this.snPdata);
			bits.ReadUInt16(out this.snNewFPO);
			bits.ReadUInt16(out this.snSectionHdrOrig);
		}

		// Token: 0x04000FBF RID: 4031
		internal ushort snFPO;

		// Token: 0x04000FC0 RID: 4032
		internal ushort snException;

		// Token: 0x04000FC1 RID: 4033
		internal ushort snFixup;

		// Token: 0x04000FC2 RID: 4034
		internal ushort snOmapToSrc;

		// Token: 0x04000FC3 RID: 4035
		internal ushort snOmapFromSrc;

		// Token: 0x04000FC4 RID: 4036
		internal ushort snSectionHdr;

		// Token: 0x04000FC5 RID: 4037
		internal ushort snTokenRidMap;

		// Token: 0x04000FC6 RID: 4038
		internal ushort snXdata;

		// Token: 0x04000FC7 RID: 4039
		internal ushort snPdata;

		// Token: 0x04000FC8 RID: 4040
		internal ushort snNewFPO;

		// Token: 0x04000FC9 RID: 4041
		internal ushort snSectionHdrOrig;
	}
}
