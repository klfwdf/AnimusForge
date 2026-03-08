using System;

namespace System.Security.Policy
{
	// Token: 0x02000359 RID: 857
	internal interface IReportMatchMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable
	{
		// Token: 0x06002A78 RID: 10872
		bool Check(Evidence evidence, out object usedEvidence);
	}
}
