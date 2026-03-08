using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x02000403 RID: 1027
	[ComVisible(true)]
	public interface ISymbolVariable
	{
		// Token: 0x17000799 RID: 1945
		// (get) Token: 0x060033C4 RID: 13252
		string Name { get; }

		// Token: 0x1700079A RID: 1946
		// (get) Token: 0x060033C5 RID: 13253
		object Attributes { get; }

		// Token: 0x060033C6 RID: 13254
		byte[] GetSignature();

		// Token: 0x1700079B RID: 1947
		// (get) Token: 0x060033C7 RID: 13255
		SymAddressKind AddressKind { get; }

		// Token: 0x1700079C RID: 1948
		// (get) Token: 0x060033C8 RID: 13256
		int AddressField1 { get; }

		// Token: 0x1700079D RID: 1949
		// (get) Token: 0x060033C9 RID: 13257
		int AddressField2 { get; }

		// Token: 0x1700079E RID: 1950
		// (get) Token: 0x060033CA RID: 13258
		int AddressField3 { get; }

		// Token: 0x1700079F RID: 1951
		// (get) Token: 0x060033CB RID: 13259
		int StartOffset { get; }

		// Token: 0x170007A0 RID: 1952
		// (get) Token: 0x060033CC RID: 13260
		int EndOffset { get; }
	}
}
