using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200042A RID: 1066
	internal class DbiModuleInfo
	{
		// Token: 0x0600176F RID: 5999 RVA: 0x000485A0 File Offset: 0x000467A0
		internal DbiModuleInfo(BitAccess bits, bool readStrings)
		{
			bits.ReadInt32(out this.opened);
			new DbiSecCon(bits);
			bits.ReadUInt16(out this.flags);
			bits.ReadInt16(out this.stream);
			bits.ReadInt32(out this.cbSyms);
			bits.ReadInt32(out this.cbOldLines);
			bits.ReadInt32(out this.cbLines);
			bits.ReadInt16(out this.files);
			bits.ReadInt16(out this.pad1);
			bits.ReadUInt32(out this.offsets);
			bits.ReadInt32(out this.niSource);
			bits.ReadInt32(out this.niCompiler);
			if (readStrings)
			{
				bits.ReadCString(out this.moduleName);
				bits.ReadCString(out this.objectName);
			}
			else
			{
				bits.SkipCString(out this.moduleName);
				bits.SkipCString(out this.objectName);
			}
			bits.Align(4);
		}

		// Token: 0x04000FDE RID: 4062
		internal int opened;

		// Token: 0x04000FDF RID: 4063
		internal ushort flags;

		// Token: 0x04000FE0 RID: 4064
		internal short stream;

		// Token: 0x04000FE1 RID: 4065
		internal int cbSyms;

		// Token: 0x04000FE2 RID: 4066
		internal int cbOldLines;

		// Token: 0x04000FE3 RID: 4067
		internal int cbLines;

		// Token: 0x04000FE4 RID: 4068
		internal short files;

		// Token: 0x04000FE5 RID: 4069
		internal short pad1;

		// Token: 0x04000FE6 RID: 4070
		internal uint offsets;

		// Token: 0x04000FE7 RID: 4071
		internal int niSource;

		// Token: 0x04000FE8 RID: 4072
		internal int niCompiler;

		// Token: 0x04000FE9 RID: 4073
		internal string moduleName;

		// Token: 0x04000FEA RID: 4074
		internal string objectName;
	}
}
