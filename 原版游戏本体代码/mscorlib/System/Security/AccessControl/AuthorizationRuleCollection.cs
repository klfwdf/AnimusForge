using System;
using System.Collections;

namespace System.Security.AccessControl
{
	// Token: 0x02000236 RID: 566
	public sealed class AuthorizationRuleCollection : ReadOnlyCollectionBase
	{
		// Token: 0x0600204A RID: 8266 RVA: 0x00071624 File Offset: 0x0006F824
		public void AddRule(AuthorizationRule rule)
		{
			base.InnerList.Add(rule);
		}

		// Token: 0x0600204B RID: 8267 RVA: 0x00071633 File Offset: 0x0006F833
		public void CopyTo(AuthorizationRule[] rules, int index)
		{
			((ICollection)this).CopyTo(rules, index);
		}

		// Token: 0x170003CA RID: 970
		public AuthorizationRule this[int index]
		{
			get
			{
				return base.InnerList[index] as AuthorizationRule;
			}
		}
	}
}
