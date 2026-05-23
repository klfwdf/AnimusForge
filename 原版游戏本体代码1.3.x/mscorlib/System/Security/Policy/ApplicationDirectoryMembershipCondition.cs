using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000342 RID: 834
	[ComVisible(true)]
	[Serializable]
	public sealed class ApplicationDirectoryMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002977 RID: 10615 RVA: 0x00099198 File Offset: 0x00097398
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002978 RID: 10616 RVA: 0x000991B0 File Offset: 0x000973B0
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			if (evidence == null)
			{
				return false;
			}
			ApplicationDirectory hostEvidence = evidence.GetHostEvidence<ApplicationDirectory>();
			Url hostEvidence2 = evidence.GetHostEvidence<Url>();
			if (hostEvidence != null && hostEvidence2 != null)
			{
				string text = hostEvidence.Directory;
				if (text != null && text.Length > 1)
				{
					if (text[text.Length - 1] == '/')
					{
						text += "*";
					}
					else
					{
						text += "/*";
					}
					URLString operand = new URLString(text);
					if (hostEvidence2.GetURLString().IsSubsetOf(operand))
					{
						usedEvidence = hostEvidence;
						return true;
					}
				}
			}
			return false;
		}

		// Token: 0x06002979 RID: 10617 RVA: 0x00099233 File Offset: 0x00097433
		public IMembershipCondition Copy()
		{
			return new ApplicationDirectoryMembershipCondition();
		}

		// Token: 0x0600297A RID: 10618 RVA: 0x0009923A File Offset: 0x0009743A
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x0600297B RID: 10619 RVA: 0x00099243 File Offset: 0x00097443
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x0600297C RID: 10620 RVA: 0x00099250 File Offset: 0x00097450
		public SecurityElement ToXml(PolicyLevel level)
		{
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.ApplicationDirectoryMembershipCondition");
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x0600297D RID: 10621 RVA: 0x0009928A File Offset: 0x0009748A
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

		// Token: 0x0600297E RID: 10622 RVA: 0x000992BC File Offset: 0x000974BC
		public override bool Equals(object o)
		{
			return o is ApplicationDirectoryMembershipCondition;
		}

		// Token: 0x0600297F RID: 10623 RVA: 0x000992C7 File Offset: 0x000974C7
		public override int GetHashCode()
		{
			return typeof(ApplicationDirectoryMembershipCondition).GetHashCode();
		}

		// Token: 0x06002980 RID: 10624 RVA: 0x000992D8 File Offset: 0x000974D8
		public override string ToString()
		{
			return Environment.GetResourceString("ApplicationDirectory_ToString");
		}
	}
}
