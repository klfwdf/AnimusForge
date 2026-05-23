using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200042E RID: 1070
	internal interface IUsedNamespace
	{
		// Token: 0x1700058C RID: 1420
		// (get) Token: 0x06001774 RID: 6004
		IName Alias { get; }

		// Token: 0x1700058D RID: 1421
		// (get) Token: 0x06001775 RID: 6005
		IName NamespaceName { get; }
	}
}
