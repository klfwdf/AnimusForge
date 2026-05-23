using System;

namespace System.Security
{
	// Token: 0x020001CC RID: 460
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
	public sealed class SecurityRulesAttribute : Attribute
	{
		// Token: 0x06001C25 RID: 7205 RVA: 0x00060EC9 File Offset: 0x0005F0C9
		public SecurityRulesAttribute(SecurityRuleSet ruleSet)
		{
			this.m_ruleSet = ruleSet;
		}

		// Token: 0x1700032C RID: 812
		// (get) Token: 0x06001C26 RID: 7206 RVA: 0x00060ED8 File Offset: 0x0005F0D8
		// (set) Token: 0x06001C27 RID: 7207 RVA: 0x00060EE0 File Offset: 0x0005F0E0
		public bool SkipVerificationInFullTrust
		{
			get
			{
				return this.m_skipVerificationInFullTrust;
			}
			set
			{
				this.m_skipVerificationInFullTrust = value;
			}
		}

		// Token: 0x1700032D RID: 813
		// (get) Token: 0x06001C28 RID: 7208 RVA: 0x00060EE9 File Offset: 0x0005F0E9
		public SecurityRuleSet RuleSet
		{
			get
			{
				return this.m_ruleSet;
			}
		}

		// Token: 0x040009CB RID: 2507
		private SecurityRuleSet m_ruleSet;

		// Token: 0x040009CC RID: 2508
		private bool m_skipVerificationInFullTrust;
	}
}
