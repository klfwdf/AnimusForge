using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x02000438 RID: 1080
	internal class PdbFunction
	{
		// Token: 0x06001797 RID: 6039 RVA: 0x00049E9C File Offset: 0x0004809C
		private static string StripNamespace(string module)
		{
			int li = module.LastIndexOf('.');
			if (li > 0)
			{
				return module.Substring(li + 1);
			}
			return module;
		}

		// Token: 0x06001798 RID: 6040 RVA: 0x00049EC4 File Offset: 0x000480C4
		internal void AdjustVisualBasicScopes()
		{
			if (!this.visualBasicScopesAdjusted)
			{
				this.visualBasicScopesAdjusted = true;
				foreach (PdbScope scope in this.scopes)
				{
					this.AdjustVisualBasicScopes(scope.scopes);
				}
			}
		}

		// Token: 0x06001799 RID: 6041 RVA: 0x00049F08 File Offset: 0x00048108
		private void AdjustVisualBasicScopes(PdbScope[] scopes)
		{
			foreach (PdbScope scope in scopes)
			{
				scope.length += 1U;
				this.AdjustVisualBasicScopes(scope.scopes);
			}
		}

		// Token: 0x0600179A RID: 6042 RVA: 0x00049F44 File Offset: 0x00048144
		internal static PdbFunction[] LoadManagedFunctions(BitAccess bits, uint limit, bool readStrings)
		{
			int begin = bits.Position;
			int count = 0;
			while ((long)bits.Position < (long)((ulong)limit))
			{
				ushort siz;
				bits.ReadUInt16(out siz);
				int star = bits.Position;
				int stop = bits.Position + (int)siz;
				bits.Position = star;
				ushort rec;
				bits.ReadUInt16(out rec);
				SYM sym = (SYM)rec;
				if (sym != SYM.S_END)
				{
					if (sym - SYM.S_GMANPROC <= 1)
					{
						ManProcSym proc;
						bits.ReadUInt32(out proc.parent);
						bits.ReadUInt32(out proc.end);
						bits.Position = (int)proc.end;
						count++;
					}
					else
					{
						bits.Position = stop;
					}
				}
				else
				{
					bits.Position = stop;
				}
			}
			if (count == 0)
			{
				return null;
			}
			bits.Position = begin;
			PdbFunction[] funcs = new PdbFunction[count];
			int func = 0;
			while ((long)bits.Position < (long)((ulong)limit))
			{
				ushort siz2;
				bits.ReadUInt16(out siz2);
				int position = bits.Position;
				int stop2 = bits.Position + (int)siz2;
				ushort rec2;
				bits.ReadUInt16(out rec2);
				SYM sym = (SYM)rec2;
				if (sym - SYM.S_GMANPROC <= 1)
				{
					ManProcSym proc2;
					bits.ReadUInt32(out proc2.parent);
					bits.ReadUInt32(out proc2.end);
					bits.ReadUInt32(out proc2.next);
					bits.ReadUInt32(out proc2.len);
					bits.ReadUInt32(out proc2.dbgStart);
					bits.ReadUInt32(out proc2.dbgEnd);
					bits.ReadUInt32(out proc2.token);
					bits.ReadUInt32(out proc2.off);
					bits.ReadUInt16(out proc2.seg);
					bits.ReadUInt8(out proc2.flags);
					bits.ReadUInt16(out proc2.retReg);
					if (readStrings)
					{
						bits.ReadCString(out proc2.name);
					}
					else
					{
						bits.SkipCString(out proc2.name);
					}
					bits.Position = stop2;
					funcs[func++] = new PdbFunction(proc2, bits);
				}
				else
				{
					bits.Position = stop2;
				}
			}
			return funcs;
		}

		// Token: 0x0600179B RID: 6043 RVA: 0x0004A114 File Offset: 0x00048314
		internal static void CountScopesAndSlots(BitAccess bits, uint limit, out int constants, out int scopes, out int slots, out int usedNamespaces)
		{
			int pos = bits.Position;
			constants = 0;
			slots = 0;
			scopes = 0;
			usedNamespaces = 0;
			while ((long)bits.Position < (long)((ulong)limit))
			{
				ushort siz;
				bits.ReadUInt16(out siz);
				int star = bits.Position;
				int stop = bits.Position + (int)siz;
				bits.Position = star;
				ushort rec;
				bits.ReadUInt16(out rec);
				SYM sym = (SYM)rec;
				if (sym <= SYM.S_MANSLOT)
				{
					if (sym == SYM.S_BLOCK32)
					{
						BlockSym32 block;
						bits.ReadUInt32(out block.parent);
						bits.ReadUInt32(out block.end);
						scopes++;
						bits.Position = (int)block.end;
						continue;
					}
					if (sym == SYM.S_MANSLOT)
					{
						slots++;
						bits.Position = stop;
						continue;
					}
				}
				else
				{
					if (sym == SYM.S_UNAMESPACE)
					{
						usedNamespaces++;
						bits.Position = stop;
						continue;
					}
					if (sym == SYM.S_MANCONSTANT)
					{
						constants++;
						bits.Position = stop;
						continue;
					}
				}
				bits.Position = stop;
			}
			bits.Position = pos;
		}

		// Token: 0x0600179C RID: 6044 RVA: 0x00002B15 File Offset: 0x00000D15
		internal PdbFunction()
		{
		}

		// Token: 0x0600179D RID: 6045 RVA: 0x0004A218 File Offset: 0x00048418
		internal PdbFunction(ManProcSym proc, BitAccess bits)
		{
			this.token = proc.token;
			this.segment = (uint)proc.seg;
			this.address = proc.off;
			this.length = proc.len;
			if (proc.seg != 1)
			{
				throw new PdbDebugException("Segment is {0}, not 1.", new object[] { proc.seg });
			}
			if (proc.parent != 0U || proc.next != 0U)
			{
				throw new PdbDebugException("Warning parent={0}, next={1}", new object[] { proc.parent, proc.next });
			}
			int constantCount;
			int scopeCount;
			int slotCount;
			int usedNamespacesCount;
			PdbFunction.CountScopesAndSlots(bits, proc.end, out constantCount, out scopeCount, out slotCount, out usedNamespacesCount);
			int scope = ((constantCount > 0 || slotCount > 0 || usedNamespacesCount > 0) ? 1 : 0);
			int slot = 0;
			int constant = 0;
			int usedNs = 0;
			this.scopes = new PdbScope[scopeCount + scope];
			this.slots = new PdbSlot[slotCount];
			this.constants = new PdbConstant[constantCount];
			this.usedNamespaces = new string[usedNamespacesCount];
			if (scope > 0)
			{
				this.scopes[0] = new PdbScope(this.address, proc.len, this.slots, this.constants, this.usedNamespaces);
			}
			while ((long)bits.Position < (long)((ulong)proc.end))
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
					if (sym != SYM.S_OEM)
					{
						if (sym == SYM.S_BLOCK32)
						{
							BlockSym32 block = default(BlockSym32);
							bits.ReadUInt32(out block.parent);
							bits.ReadUInt32(out block.end);
							bits.ReadUInt32(out block.len);
							bits.ReadUInt32(out block.off);
							bits.ReadUInt16(out block.seg);
							bits.SkipCString(out block.name);
							bits.Position = stop;
							this.scopes[scope++] = new PdbScope(this.address, block, bits, ref this.slotToken);
							bits.Position = (int)block.end;
							continue;
						}
					}
					else
					{
						OemSymbol oem;
						bits.ReadGuid(out oem.idOem);
						bits.ReadUInt32(out oem.typind);
						if (oem.idOem == PdbFunction.msilMetaData)
						{
							string name = bits.ReadString();
							if (name == "MD2")
							{
								this.ReadMD2CustomMetadata(bits);
							}
							else if (name == "asyncMethodInfo")
							{
								this.synchronizationInformation = new PdbSynchronizationInformation(bits);
							}
							bits.Position = stop;
							continue;
						}
						throw new PdbDebugException("OEM section: guid={0} ti={1}", new object[] { oem.idOem, oem.typind });
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
			if ((long)bits.Position != (long)((ulong)proc.end))
			{
				throw new PdbDebugException("Not at S_END", new object[0]);
			}
			ushort esiz;
			bits.ReadUInt16(out esiz);
			ushort erec;
			bits.ReadUInt16(out erec);
			if (erec != 6)
			{
				throw new PdbDebugException("Missing S_END", new object[0]);
			}
		}

		// Token: 0x0600179E RID: 6046 RVA: 0x0004A5D8 File Offset: 0x000487D8
		internal void ReadMD2CustomMetadata(BitAccess bits)
		{
			byte version;
			bits.ReadUInt8(out version);
			if (version == 4)
			{
				byte count;
				bits.ReadUInt8(out count);
				bits.Align(4);
				for (;;)
				{
					byte b = count;
					count = b - 1;
					if (b <= 0)
					{
						break;
					}
					this.ReadCustomMetadata(bits);
				}
			}
		}

		// Token: 0x0600179F RID: 6047 RVA: 0x0004A614 File Offset: 0x00048814
		private void ReadCustomMetadata(BitAccess bits)
		{
			int savedPosition = bits.Position;
			byte version;
			bits.ReadUInt8(out version);
			byte kind;
			bits.ReadUInt8(out kind);
			bits.Position += 2;
			uint numberOfBytesInItem;
			bits.ReadUInt32(out numberOfBytesInItem);
			if (version == 4)
			{
				switch (kind)
				{
				case 0:
					this.ReadUsingInfo(bits);
					break;
				case 1:
					this.ReadForwardInfo(bits);
					break;
				case 3:
					this.ReadIteratorLocals(bits);
					break;
				case 4:
					this.ReadForwardIterator(bits);
					break;
				}
			}
			bits.Position = savedPosition + (int)numberOfBytesInItem;
		}

		// Token: 0x060017A0 RID: 6048 RVA: 0x0004A6A5 File Offset: 0x000488A5
		private void ReadForwardIterator(BitAccess bits)
		{
			this.iteratorClass = bits.ReadString();
		}

		// Token: 0x060017A1 RID: 6049 RVA: 0x0004A6B4 File Offset: 0x000488B4
		private void ReadIteratorLocals(BitAccess bits)
		{
			uint numberOfLocals;
			bits.ReadUInt32(out numberOfLocals);
			this.iteratorScopes = new List<ILocalScope>((int)numberOfLocals);
			while (numberOfLocals-- > 0U)
			{
				uint ilStartOffset;
				bits.ReadUInt32(out ilStartOffset);
				uint ilEndOffset;
				bits.ReadUInt32(out ilEndOffset);
				this.iteratorScopes.Add(new PdbIteratorScope(ilStartOffset, ilEndOffset - ilStartOffset));
			}
		}

		// Token: 0x060017A2 RID: 6050 RVA: 0x0004A703 File Offset: 0x00048903
		private void ReadForwardInfo(BitAccess bits)
		{
			bits.ReadUInt32(out this.tokenOfMethodWhoseUsingInfoAppliesToThisMethod);
		}

		// Token: 0x060017A3 RID: 6051 RVA: 0x0004A714 File Offset: 0x00048914
		private void ReadUsingInfo(BitAccess bits)
		{
			ushort numberOfNamespaces;
			bits.ReadUInt16(out numberOfNamespaces);
			this.usingCounts = new ushort[(int)numberOfNamespaces];
			for (ushort i = 0; i < numberOfNamespaces; i += 1)
			{
				bits.ReadUInt16(out this.usingCounts[(int)i]);
			}
		}

		// Token: 0x0400100C RID: 4108
		internal static readonly Guid msilMetaData = new Guid(3337240521U, 22963, 18902, 188, 37, 9, 2, 187, 171, 180, 96);

		// Token: 0x0400100D RID: 4109
		internal static readonly IComparer byAddress = new PdbFunction.PdbFunctionsByAddress();

		// Token: 0x0400100E RID: 4110
		internal static readonly IComparer byAddressAndToken = new PdbFunction.PdbFunctionsByAddressAndToken();

		// Token: 0x0400100F RID: 4111
		internal uint token;

		// Token: 0x04001010 RID: 4112
		internal uint slotToken;

		// Token: 0x04001011 RID: 4113
		internal uint tokenOfMethodWhoseUsingInfoAppliesToThisMethod;

		// Token: 0x04001012 RID: 4114
		internal uint segment;

		// Token: 0x04001013 RID: 4115
		internal uint address;

		// Token: 0x04001014 RID: 4116
		internal uint length;

		// Token: 0x04001015 RID: 4117
		internal PdbScope[] scopes;

		// Token: 0x04001016 RID: 4118
		internal PdbSlot[] slots;

		// Token: 0x04001017 RID: 4119
		internal PdbConstant[] constants;

		// Token: 0x04001018 RID: 4120
		internal string[] usedNamespaces;

		// Token: 0x04001019 RID: 4121
		internal PdbLines[] lines;

		// Token: 0x0400101A RID: 4122
		internal ushort[] usingCounts;

		// Token: 0x0400101B RID: 4123
		internal IEnumerable<INamespaceScope> namespaceScopes;

		// Token: 0x0400101C RID: 4124
		internal string iteratorClass;

		// Token: 0x0400101D RID: 4125
		internal List<ILocalScope> iteratorScopes;

		// Token: 0x0400101E RID: 4126
		internal PdbSynchronizationInformation synchronizationInformation;

		// Token: 0x0400101F RID: 4127
		private bool visualBasicScopesAdjusted;

		// Token: 0x02000439 RID: 1081
		internal class PdbFunctionsByAddress : IComparer
		{
			// Token: 0x060017A5 RID: 6053 RVA: 0x0004A7AC File Offset: 0x000489AC
			public int Compare(object x, object y)
			{
				PdbFunction fx = (PdbFunction)x;
				PdbFunction fy = (PdbFunction)y;
				if (fx.segment < fy.segment)
				{
					return -1;
				}
				if (fx.segment > fy.segment)
				{
					return 1;
				}
				if (fx.address < fy.address)
				{
					return -1;
				}
				if (fx.address > fy.address)
				{
					return 1;
				}
				return 0;
			}
		}

		// Token: 0x0200043A RID: 1082
		internal class PdbFunctionsByAddressAndToken : IComparer
		{
			// Token: 0x060017A7 RID: 6055 RVA: 0x0004A808 File Offset: 0x00048A08
			public int Compare(object x, object y)
			{
				PdbFunction fx = (PdbFunction)x;
				PdbFunction fy = (PdbFunction)y;
				if (fx.segment < fy.segment)
				{
					return -1;
				}
				if (fx.segment > fy.segment)
				{
					return 1;
				}
				if (fx.address < fy.address)
				{
					return -1;
				}
				if (fx.address > fy.address)
				{
					return 1;
				}
				if (fx.token < fy.token)
				{
					return -1;
				}
				if (fx.token > fy.token)
				{
					return 1;
				}
				return 0;
			}
		}
	}
}
