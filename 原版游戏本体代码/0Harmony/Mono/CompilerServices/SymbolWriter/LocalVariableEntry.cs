using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200033C RID: 828
	internal struct LocalVariableEntry
	{
		// Token: 0x06001529 RID: 5417 RVA: 0x0004312F File Offset: 0x0004132F
		public LocalVariableEntry(int index, string name, int block)
		{
			this.Index = index;
			this.Name = name;
			this.BlockIndex = block;
		}

		// Token: 0x0600152A RID: 5418 RVA: 0x00043146 File Offset: 0x00041346
		internal LocalVariableEntry(MonoSymbolFile file, MyBinaryReader reader)
		{
			this.Index = reader.ReadLeb128();
			this.Name = reader.ReadString();
			this.BlockIndex = reader.ReadLeb128();
		}

		// Token: 0x0600152B RID: 5419 RVA: 0x0004316C File Offset: 0x0004136C
		internal void Write(MonoSymbolFile file, MyBinaryWriter bw)
		{
			bw.WriteLeb128(this.Index);
			bw.Write(this.Name);
			bw.WriteLeb128(this.BlockIndex);
		}

		// Token: 0x0600152C RID: 5420 RVA: 0x00043192 File Offset: 0x00041392
		public override string ToString()
		{
			return string.Format("[LocalVariable {0}:{1}:{2}]", this.Name, this.Index, this.BlockIndex - 1);
		}

		// Token: 0x04000AB8 RID: 2744
		public readonly int Index;

		// Token: 0x04000AB9 RID: 2745
		public readonly string Name;

		// Token: 0x04000ABA RID: 2746
		public readonly int BlockIndex;
	}
}
