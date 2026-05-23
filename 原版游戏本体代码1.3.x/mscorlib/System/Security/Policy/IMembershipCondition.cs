using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x02000358 RID: 856
	[ComVisible(true)]
	public interface IMembershipCondition : ISecurityEncodable, ISecurityPolicyEncodable
	{
		// Token: 0x06002A74 RID: 10868
		bool Check(Evidence evidence);

		// Token: 0x06002A75 RID: 10869
		IMembershipCondition Copy();

		// Token: 0x06002A76 RID: 10870
		string ToString();

		// Token: 0x06002A77 RID: 10871
		bool Equals(object obj);
	}
}
