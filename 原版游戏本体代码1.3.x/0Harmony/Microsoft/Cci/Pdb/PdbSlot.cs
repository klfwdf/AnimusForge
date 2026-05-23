using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000442 RID: 1090
	internal class PdbSlot
	{
		// Token: 0x060017B8 RID: 6072 RVA: 0x0004AC54 File Offset: 0x00048E54
		internal PdbSlot(uint slot, uint typeToken, string name, ushort flags)
		{
			this.slot = slot;
			this.typeToken = typeToken;
			this.name = name;
			this.flags = flags;
		}

		// Token: 0x060017B9 RID: 6073 RVA: 0x0004AC7C File Offset: 0x00048E7C
		internal PdbSlot(BitAccess bits)
		{
			AttrSlotSym slot;
			bits.ReadUInt32(out slot.index);
			bits.ReadUInt32(out slot.typind);
			bits.ReadUInt32(out slot.offCod);
			bits.ReadUInt16(out slot.segCod);
			bits.ReadUInt16(out slot.flags);
			bits.ReadCString(out slot.name);
			this.slot = slot.index;
			this.typeToken = slot.typind;
			this.name = slot.name;
			this.flags = slot.flags;
		}

		// Token: 0x0400103C RID: 4156
		internal uint slot;

		// Token: 0x0400103D RID: 4157
		internal uint typeToken;

		// Token: 0x0400103E RID: 4158
		internal string name;

		// Token: 0x0400103F RID: 4159
		internal ushort flags;
	}
}
