using System;

namespace System.Security.Policy
{
	// Token: 0x02000357 RID: 855
	internal interface IDelayEvaluatedEvidence
	{
		// Token: 0x170005A1 RID: 1441
		// (get) Token: 0x06002A71 RID: 10865
		bool IsVerified
		{
			[SecurityCritical]
			get;
		}

		// Token: 0x170005A2 RID: 1442
		// (get) Token: 0x06002A72 RID: 10866
		bool WasUsed { get; }

		// Token: 0x06002A73 RID: 10867
		void MarkUsed();
	}
}
