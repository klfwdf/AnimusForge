using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200033F RID: 831
	[ComVisible(true)]
	[Serializable]
	public sealed class AllMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x0600295F RID: 10591 RVA: 0x00098F38 File Offset: 0x00097138
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002960 RID: 10592 RVA: 0x00098F50 File Offset: 0x00097150
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			return true;
		}

		// Token: 0x06002961 RID: 10593 RVA: 0x00098F56 File Offset: 0x00097156
		public IMembershipCondition Copy()
		{
			return new AllMembershipCondition();
		}

		// Token: 0x06002962 RID: 10594 RVA: 0x00098F5D File Offset: 0x0009715D
		public override string ToString()
		{
			return Environment.GetResourceString("All_ToString");
		}

		// Token: 0x06002963 RID: 10595 RVA: 0x00098F69 File Offset: 0x00097169
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002964 RID: 10596 RVA: 0x00098F72 File Offset: 0x00097172
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002965 RID: 10597 RVA: 0x00098F7C File Offset: 0x0009717C
		public SecurityElement ToXml(PolicyLevel level)
		{
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.AllMembershipCondition");
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06002966 RID: 10598 RVA: 0x00098FB6 File Offset: 0x000971B6
		public void FromXml(SecurityElement e, PolicyLevel level)
		{
			if (e == null)
			{
				throw new ArgumentNullException("e");
			}
			if (!e.Tag.Equals("IMembershipCondition"))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_MembershipConditionElement"));
			}
		}

		// Token: 0x06002967 RID: 10599 RVA: 0x00098FE8 File Offset: 0x000971E8
		public override bool Equals(object o)
		{
			return o is AllMembershipCondition;
		}

		// Token: 0x06002968 RID: 10600 RVA: 0x00098FF3 File Offset: 0x000971F3
		public override int GetHashCode()
		{
			return typeof(AllMembershipCondition).GetHashCode();
		}
	}
}
