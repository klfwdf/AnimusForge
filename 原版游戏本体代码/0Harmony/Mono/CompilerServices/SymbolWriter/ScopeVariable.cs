using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x02000340 RID: 832
	internal struct ScopeVariable
	{
		// Token: 0x06001535 RID: 5429 RVA: 0x000432A3 File Offset: 0x000414A3
		public ScopeVariable(int scope, int index)
		{
			this.Scope = scope;
			this.Index = index;
		}

		// Token: 0x06001536 RID: 5430 RVA: 0x000432B3 File Offset: 0x000414B3
		internal ScopeVariable(MyBinaryReader reader)
		{
			this.Scope = reader.ReadLeb128();
			this.Index = reader.ReadLeb128();
		}

		// Token: 0x06001537 RID: 5431 RVA: 0x000432CD File Offset: 0x000414CD
		internal void Write(MyBinaryWriter bw)
		{
			bw.WriteLeb128(this.Scope);
			bw.WriteLeb128(this.Index);
		}

		// Token: 0x06001538 RID: 5432 RVA: 0x000432E7 File Offset: 0x000414E7
		public override string ToString()
		{
			return string.Format("[ScopeVariable {0}:{1}]", this.Scope, this.Index);
		}

		// Token: 0x04000AC4 RID: 2756
		public readonly int Scope;

		// Token: 0x04000AC5 RID: 2757
		public readonly int Index;
	}
}
