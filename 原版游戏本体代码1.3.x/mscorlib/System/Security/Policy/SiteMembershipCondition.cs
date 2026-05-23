using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200036B RID: 875
	[ComVisible(true)]
	[Serializable]
	public sealed class SiteMembershipCondition : IMembershipCondition, ISecurityEncodable, ISecurityPolicyEncodable, IConstantMembershipCondition, IReportMatchMembershipCondition
	{
		// Token: 0x06002B42 RID: 11074 RVA: 0x000A14EC File Offset: 0x0009F6EC
		internal SiteMembershipCondition()
		{
			this.m_site = null;
		}

		// Token: 0x06002B43 RID: 11075 RVA: 0x000A14FB File Offset: 0x0009F6FB
		public SiteMembershipCondition(string site)
		{
			if (site == null)
			{
				throw new ArgumentNullException("site");
			}
			this.m_site = new SiteString(site);
		}

		// Token: 0x170005C8 RID: 1480
		// (get) Token: 0x06002B45 RID: 11077 RVA: 0x000A1539 File Offset: 0x0009F739
		// (set) Token: 0x06002B44 RID: 11076 RVA: 0x000A151D File Offset: 0x0009F71D
		public string Site
		{
			get
			{
				if (this.m_site == null && this.m_element != null)
				{
					this.ParseSite();
				}
				if (this.m_site != null)
				{
					return this.m_site.ToString();
				}
				return "";
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("value");
				}
				this.m_site = new SiteString(value);
			}
		}

		// Token: 0x06002B46 RID: 11078 RVA: 0x000A156C File Offset: 0x0009F76C
		public bool Check(Evidence evidence)
		{
			object obj = null;
			return ((IReportMatchMembershipCondition)this).Check(evidence, out obj);
		}

		// Token: 0x06002B47 RID: 11079 RVA: 0x000A1584 File Offset: 0x0009F784
		bool IReportMatchMembershipCondition.Check(Evidence evidence, out object usedEvidence)
		{
			usedEvidence = null;
			if (evidence == null)
			{
				return false;
			}
			Site hostEvidence = evidence.GetHostEvidence<Site>();
			if (hostEvidence != null)
			{
				if (this.m_site == null && this.m_element != null)
				{
					this.ParseSite();
				}
				if (hostEvidence.GetSiteString().IsSubsetOf(this.m_site))
				{
					usedEvidence = hostEvidence;
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002B48 RID: 11080 RVA: 0x000A15D2 File Offset: 0x0009F7D2
		public IMembershipCondition Copy()
		{
			if (this.m_site == null && this.m_element != null)
			{
				this.ParseSite();
			}
			return new SiteMembershipCondition(this.m_site.ToString());
		}

		// Token: 0x06002B49 RID: 11081 RVA: 0x000A15FA File Offset: 0x0009F7FA
		public SecurityElement ToXml()
		{
			return this.ToXml(null);
		}

		// Token: 0x06002B4A RID: 11082 RVA: 0x000A1603 File Offset: 0x0009F803
		public void FromXml(SecurityElement e)
		{
			this.FromXml(e, null);
		}

		// Token: 0x06002B4B RID: 11083 RVA: 0x000A1610 File Offset: 0x0009F810
		public SecurityElement ToXml(PolicyLevel level)
		{
			if (this.m_site == null && this.m_element != null)
			{
				this.ParseSite();
			}
			SecurityElement securityElement = new SecurityElement("IMembershipCondition");
			XMLUtil.AddClassAttribute(securityElement, base.GetType(), "System.Security.Policy.SiteMembershipCondition");
			securityElement.AddAttribute("version", "1");
			if (this.m_site != null)
			{
				securityElement.AddAttribute("Site", this.m_site.ToString());
			}
			return securityElement;
		}

		// Token: 0x06002B4C RID: 11084 RVA: 0x000A1680 File Offset: 0x0009F880
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
			lock (this)
			{
				this.m_site = null;
				this.m_element = e;
			}
		}

		// Token: 0x06002B4D RID: 11085 RVA: 0x000A16F4 File Offset: 0x0009F8F4
		private void ParseSite()
		{
			lock (this)
			{
				if (this.m_element != null)
				{
					string text = this.m_element.Attribute("Site");
					if (text == null)
					{
						throw new ArgumentException(Environment.GetResourceString("Argument_SiteCannotBeNull"));
					}
					this.m_site = new SiteString(text);
					this.m_element = null;
				}
			}
		}

		// Token: 0x06002B4E RID: 11086 RVA: 0x000A176C File Offset: 0x0009F96C
		public override bool Equals(object o)
		{
			SiteMembershipCondition siteMembershipCondition = o as SiteMembershipCondition;
			if (siteMembershipCondition != null)
			{
				if (this.m_site == null && this.m_element != null)
				{
					this.ParseSite();
				}
				if (siteMembershipCondition.m_site == null && siteMembershipCondition.m_element != null)
				{
					siteMembershipCondition.ParseSite();
				}
				if (object.Equals(this.m_site, siteMembershipCondition.m_site))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06002B4F RID: 11087 RVA: 0x000A17C5 File Offset: 0x0009F9C5
		public override int GetHashCode()
		{
			if (this.m_site == null && this.m_element != null)
			{
				this.ParseSite();
			}
			if (this.m_site != null)
			{
				return this.m_site.GetHashCode();
			}
			return typeof(SiteMembershipCondition).GetHashCode();
		}

		// Token: 0x06002B50 RID: 11088 RVA: 0x000A1800 File Offset: 0x0009FA00
		public override string ToString()
		{
			if (this.m_site == null && this.m_element != null)
			{
				this.ParseSite();
			}
			if (this.m_site != null)
			{
				return string.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Site_ToStringArg"), this.m_site);
			}
			return Environment.GetResourceString("Site_ToString");
		}

		// Token: 0x0400119A RID: 4506
		private SiteString m_site;

		// Token: 0x0400119B RID: 4507
		private SecurityElement m_element;
	}
}
