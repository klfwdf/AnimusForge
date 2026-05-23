using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000441 RID: 1089
	internal class PdbScope
	{
		// Token: 0x060017B5 RID: 6069 RVA: 0x0004A9B8 File Offset: 0x00048BB8
		internal PdbScope(uint address, uint offset, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces)
		{
			this.constants = constants;
			this.slots = slots;
			this.scopes = new PdbScope[0];
			this.usedNamespaces = usedNamespaces;
			this.address = address;
			this.offset = offset;
			this.length = length;
		}

		// Token: 0x060017B6 RID: 6070 RVA: 0x0004AA04 File Offset: 0x00048C04
		internal PdbScope(uint address, uint length, PdbSlot[] slots, PdbConstant[] constants, string[] usedNamespaces)
			: this(address, 0U, length, slots, constants, usedNamespaces)
		{
		}

		// Token: 0x060017B7 RID: 6071 RVA: 0x0004AA14 File Offset: 0x00048C14
		internal PdbScope(uint funcOffset, BlockSym32 block, BitAccess bits, out uint typind)
		{
			this.address = block.off;
			this.offset = block.off - funcOffset;
			this.length = block.len;
			typind = 0U;
			int constantCount;
			int scopeCount;
			int slotCount;
			int namespaceCount;
			PdbFunction.CountScopesAndSlots(bits, block.end, out constantCount, out scopeCount, out slotCount, out namespaceCount);
			this.constants = new PdbConstant[constantCount];
			this.scopes = new PdbScope[scopeCount];
			this.slots = new PdbSlot[slotCount];
			this.usedNamespaces = new string[namespaceCount];
			int constant = 0;
			int scope = 0;
			int slot = 0;
			int usedNs = 0;
			while ((long)bits.Position < (long)((ulong)block.end))
			{
				ushort siz;
				bits.ReadUInt16(out siz);
				int star = bits.Position;
				int stop = bits.Position + (int)siz;
				bits.Position = star;
				ushort rec;
				bits.ReadUInt16(out rec);
				SYM sym = (SYM)rec;
				if (sym <= SYM.S_BLOCK32)
				{
					if (sym == SYM.S_END)
					{
						bits.Position = stop;
						continue;
					}
					if (sym == SYM.S_BLOCK32)
					{
						BlockSym32 sub = default(BlockSym32);
						bits.ReadUInt32(out sub.parent);
						bits.ReadUInt32(out sub.end);
						bits.ReadUInt32(out sub.len);
						bits.ReadUInt32(out sub.off);
						bits.ReadUInt16(out sub.seg);
						bits.SkipCString(out sub.name);
						bits.Position = stop;
						this.scopes[scope++] = new PdbScope(funcOffset, sub, bits, ref typind);
						continue;
					}
				}
				else
				{
					if (sym == SYM.S_MANSLOT)
					{
						this.slots[slot++] = new PdbSlot(bits);
						bits.Position = stop;
						continue;
					}
					if (sym == SYM.S_UNAMESPACE)
					{
						bits.ReadCString(out this.usedNamespaces[usedNs++]);
						bits.Position = stop;
						continue;
					}
					if (sym == SYM.S_MANCONSTANT)
					{
						this.constants[constant++] = new PdbConstant(bits);
						bits.Position = stop;
						continue;
					}
				}
				bits.Position = stop;
			}
			if ((long)bits.Position != (long)((ulong)block.end))
			{
				throw new Exception("Not at S_END");
			}
			ushort esiz;
			bits.ReadUInt16(out esiz);
			ushort erec;
			bits.ReadUInt16(out erec);
			if (erec != 6)
			{
				throw new Exception("Missing S_END");
			}
		}

		// Token: 0x04001035 RID: 4149
		internal PdbConstant[] constants;

		// Token: 0x04001036 RID: 4150
		internal PdbSlot[] slots;

		// Token: 0x04001037 RID: 4151
		internal PdbScope[] scopes;

		// Token: 0x04001038 RID: 4152
		internal string[] usedNamespaces;

		// Token: 0x04001039 RID: 4153
		internal uint address;

		// Token: 0x0400103A RID: 4154
		internal uint offset;

		// Token: 0x0400103B RID: 4155
		internal uint length;
	}
}
