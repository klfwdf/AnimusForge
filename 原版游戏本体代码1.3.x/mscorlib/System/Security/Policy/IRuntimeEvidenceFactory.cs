using System;
using System.Collections.Generic;

namespace System.Security.Policy
{
	// Token: 0x0200035A RID: 858
	internal interface IRuntimeEvidenceFactory
	{
		// Token: 0x170005A3 RID: 1443
		// (get) Token: 0x06002A79 RID: 10873
		IEvidenceFactory Target { get; }

		// Token: 0x06002A7A RID: 10874
		IEnumerable<EvidenceBase> GetFactorySuppliedEvidence();

		// Token: 0x06002A7B RID: 10875
		EvidenceBase GenerateEvidence(Type evidenceType);
	}
}
