using System;
using System.Runtime.InteropServices;

namespace System.Diagnostics.SymbolStore
{
	// Token: 0x02000400 RID: 1024
	[ComVisible(true)]
	public interface ISymbolNamespace
	{
		// Token: 0x17000793 RID: 1939
		// (get) Token: 0x060033B0 RID: 13232
		string Name { get; }

		// Token: 0x060033B1 RID: 13233
		ISymbolNamespace[] GetNamespaces();

		// Token: 0x060033B2 RID: 13234
		ISymbolVariable[] GetVariables();
	}
}
