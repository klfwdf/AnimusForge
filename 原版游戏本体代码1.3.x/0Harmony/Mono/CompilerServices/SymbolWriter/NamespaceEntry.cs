using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000347 RID: 839
	internal struct NamespaceEntry
	{
		// Token: 0x06001573 RID: 5491 RVA: 0x00044AA0 File Offset: 0x00042CA0
		public NamespaceEntry(string name, int index, string[] using_clauses, int parent)
		{
			this.Name = name;
			this.Index = index;
			this.Parent = parent;
			this.UsingClauses = ((using_clauses != null) ? using_clauses : new string[0]);
		}

		// Token: 0x06001574 RID: 5492 RVA: 0x00044ACC File Offset: 0x00042CCC
		internal NamespaceEntry(MonoSymbolFile file, MyBinaryReader reader)
		{
			this.Name = reader.ReadString();
			this.Index = reader.ReadLeb128();
			this.Parent = reader.ReadLeb128();
			int count = reader.ReadLeb128();
			this.UsingClauses = new string[count];
			for (int i = 0; i < count; i++)
			{
				this.UsingClauses[i] = reader.ReadString();
			}
		}

		// Token: 0x06001575 RID: 5493 RVA: 0x00044B2C File Offset: 0x00042D2C
		internal void Write(MonoSymbolFile file, MyBinaryWriter bw)
		{
			bw.Write(this.Name);
			bw.WriteLeb128(this.Index);
			bw.WriteLeb128(this.Parent);
			bw.WriteLeb128(this.UsingClauses.Length);
			foreach (string uc in this.UsingClauses)
			{
				bw.Write(uc);
			}
		}

		// Token: 0x06001576 RID: 5494 RVA: 0x00044B8B File Offset: 0x00042D8B
		public override string ToString()
		{
			return string.Format("[Namespace {0}:{1}:{2}]", this.Name, this.Index, this.Parent);
		}

		// Token: 0x04000B01 RID: 2817
		public readonly string Name;

		// Token: 0x04000B02 RID: 2818
		public readonly int Index;

		// Token: 0x04000B03 RID: 2819
		public readonly int Parent;

		// Token: 0x04000B04 RID: 2820
		public readonly string[] UsingClauses;
	}
}
