using System;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200033D RID: 829
	internal struct CapturedVariable
	{
		// Token: 0x0600152D RID: 5421 RVA: 0x000431BC File Offset: 0x000413BC
		public CapturedVariable(string name, string captured_name, CapturedVariable.CapturedKind kind)
		{
			this.Name = name;
			this.CapturedName = captured_name;
			this.Kind = kind;
		}

		// Token: 0x0600152E RID: 5422 RVA: 0x000431D3 File Offset: 0x000413D3
		internal CapturedVariable(MyBinaryReader reader)
		{
			this.Name = reader.ReadString();
			this.CapturedName = reader.ReadString();
			this.Kind = (CapturedVariable.CapturedKind)reader.ReadByte();
		}

		// Token: 0x0600152F RID: 5423 RVA: 0x000431F9 File Offset: 0x000413F9
		internal void Write(MyBinaryWriter bw)
		{
			bw.Write(this.Name);
			bw.Write(this.CapturedName);
			bw.Write((byte)this.Kind);
		}

		// Token: 0x06001530 RID: 5424 RVA: 0x0004321F File Offset: 0x0004141F
		public override string ToString()
		{
			return string.Format("[CapturedVariable {0}:{1}:{2}]", this.Name, this.CapturedName, this.Kind);
		}

		// Token: 0x04000ABB RID: 2747
		public readonly string Name;

		// Token: 0x04000ABC RID: 2748
		public readonly string CapturedName;

		// Token: 0x04000ABD RID: 2749
		public readonly CapturedVariable.CapturedKind Kind;

		// Token: 0x0200033E RID: 830
		public enum CapturedKind : byte
		{
			// Token: 0x04000ABF RID: 2751
			Local,
			// Token: 0x04000AC0 RID: 2752
			Parameter,
			// Token: 0x04000AC1 RID: 2753
			This
		}
	}
}
