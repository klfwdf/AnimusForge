using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200033F RID: 831
	internal struct CapturedScope
	{
		// Token: 0x06001531 RID: 5425 RVA: 0x00043242 File Offset: 0x00041442
		public CapturedScope(int scope, string captured_name)
		{
			this.Scope = scope;
			this.CapturedName = captured_name;
		}

		// Token: 0x06001532 RID: 5426 RVA: 0x00043252 File Offset: 0x00041452
		internal CapturedScope(MyBinaryReader reader)
		{
			this.Scope = reader.ReadLeb128();
			this.CapturedName = reader.ReadString();
		}

		// Token: 0x06001533 RID: 5427 RVA: 0x0004326C File Offset: 0x0004146C
		internal void Write(MyBinaryWriter bw)
		{
			bw.WriteLeb128(this.Scope);
			bw.Write(this.CapturedName);
		}

		// Token: 0x06001534 RID: 5428 RVA: 0x00043286 File Offset: 0x00041486
		public override string ToString()
		{
			return string.Format("[CapturedScope {0}:{1}]", this.Scope, this.CapturedName);
		}

		// Token: 0x04000AC2 RID: 2754
		public readonly int Scope;

		// Token: 0x04000AC3 RID: 2755
		public readonly string CapturedName;
	}
}
