using System;
using System.Collections.Generic;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200042D RID: 1069
	internal interface INamespaceScope
	{
		// Token: 0x1700058B RID: 1419
		// (get) Token: 0x06001773 RID: 6003
		IEnumerable<IUsedNamespace> UsedNamespaces { get; }
	}
}
