using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x02000374 RID: 884
	[ComVisible(true)]
	[Serializable]
	public sealed class GacMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002BC7 RID: 11207 RVA: 0x000A302C File Offset: 0x000A122C
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002BC8 RID: 11208 RVA: 0x000A3044 File Offset: 0x000A1244
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			return evidence != null && evidence.GetHostEvidence<GacInstalled>() != null;
		}

		// Token: 0x06002BC9 RID: 11209 RVA: 0x000A3057 File Offset: 0x000A1257
		public IMembershipCondition Copy()
		{
			return new GacMembershipCondition();
		}

		// Token: 0x06002BCA RID: 11210 RVA: 0x000A305E File Offset: 0x000A125E
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002BCB RID: 11211 RVA: 0x000A3067 File Offset: 0x000A1267
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002BCC RID: 11212 RVA: 0x000A3074 File Offset: 0x000A1274
		public SecurityElement ToXml(PolicyLevel level)
		{
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), base.GetType().FullName);
			securityElement.AddAttribute("version", "1");
			return securityElement;
		}

		// Token: 0x06002BCD RID: 11213 RVA: 0x000A30B4 File Offset: 0x000A12B4
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

		// Token: 0x06002BCE RID: 11214 RVA: 0x000A30E8 File Offset: 0x000A12E8
		public override bool Equals(object o)
		{
			return o is GacMembershipCondition;
		}

		// Token: 0x06002BCF RID: 11215 RVA: 0x000A3102 File Offset: 0x000A1302
		public override int GetHashCode()
		{
			return 0;
		}

		// Token: 0x06002BD0 RID: 11216 RVA: 0x000A3105 File Offset: 0x000A1305
		public override string ToString()
		{
			return Environment.GetResourceString("GAC_ToString");
		}
	}
}
