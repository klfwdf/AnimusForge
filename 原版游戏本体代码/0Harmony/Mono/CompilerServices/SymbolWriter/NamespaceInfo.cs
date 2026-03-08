using System;
using System.Collections;

namespace Mono.CompilerServices.SymbolWriter
{
	// Token: 0x0200034D RID: 845
	internal class NamespaceInfo
	{
		// Token: 0x04000B20 RID: 2848
		public string Name;

		// Token: 0x04000B21 RID: 2849
		public int NamespaceID;

		// Token: 0x04000B22 RID: 2850
		public ArrayList UsingClauses = new ArrayList();
	}
}
