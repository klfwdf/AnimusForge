using System;

namespace Microsoft.Cci.Pdb
{
	// Token: 0x0200042F RID: 1071
	internal interface IName
	{
		// Token: 0x1700058E RID: 1422
		// (get) Token: 0x06001776 RID: 6006
		int UniqueKey { get; }

		// Token: 0x1700058F RID: 1423
		// (get) Token: 0x06001777 RID: 6007
		int UniqueKeyIgnoringCase { get; }

		// Token: 0x17000590 RID: 1424
		// (get) Token: 0x06001778 RID: 6008
		string Value { get; }
	}
}
