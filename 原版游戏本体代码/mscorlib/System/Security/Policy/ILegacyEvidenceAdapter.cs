using System;

namespace System.Security.Policy
{
	// Token: 0x0200034F RID: 847
	internal interface ILegacyEvidenceAdapter
	{
		// Token: 0x17000593 RID: 1427
		// (get) Token: 0x06002A3F RID: 10815
		object EvidenceObject { get; }

		// Token: 0x17000594 RID: 1428
		// (get) Token: 0x06002A40 RID: 10816
		Type EvidenceType { get; }
	}
}
