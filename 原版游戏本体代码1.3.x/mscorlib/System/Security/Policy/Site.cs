using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200036A RID: 874
	[ComVisible(true)]
	[Serializable]
	public sealed class Site : EvidenceBase, IIdentityPermissionFactory
	{
		// Token: 0x06002B34 RID: 11060 RVA: 0x000A137F File Offset: 0x0009F57F
		public Site(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.m_name = new SiteString(name);
		}

		// Token: 0x06002B35 RID: 11061 RVA: 0x000A13A1 File Offset: 0x0009F5A1
		private Site(SiteString name)
		{
			this.m_name = name;
		}

		// Token: 0x06002B36 RID: 11062 RVA: 0x000A13B0 File Offset: 0x0009F5B0
		public static Site CreateFromUrl(string url)
		{
			return new Site(Site.ParseSiteFromUrl(url));
		}

		// Token: 0x06002B37 RID: 11063 RVA: 0x000A13C0 File Offset: 0x0009F5C0
		private static SiteString ParseSiteFromUrl(string name)
		{
			URLString urlstring = new URLString(name);
			if (string.Compare(urlstring.Scheme, "file", StringComparison.OrdinalIgnoreCase) == 0)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidSite"));
			}
			return new SiteString(new URLString(name).Host);
		}

		// Token: 0x170005C7 RID: 1479
		// (get) Token: 0x06002B38 RID: 11064 RVA: 0x000A1407 File Offset: 0x0009F607
		public string Name
		{
			get
			{
				return this.m_name.ToString();
			}
		}

		// Token: 0x06002B39 RID: 11065 RVA: 0x000A1414 File Offset: 0x0009F614
		internal SiteString GetSiteString()
		{
			return this.m_name;
		}

		// Token: 0x06002B3A RID: 11066 RVA: 0x000A141C File Offset: 0x0009F61C
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new SiteIdentityPermission(this.Name);
		}

		// Token: 0x06002B3B RID: 11067 RVA: 0x000A142C File Offset: 0x0009F62C
		public override bool Equals(object o)
		{
			Site site = o as Site;
			return site != null && string.Equals(this.Name, site.Name, StringComparison.OrdinalIgnoreCase);
		}

		// Token: 0x06002B3C RID: 11068 RVA: 0x000A1457 File Offset: 0x0009F657
		public override int GetHashCode()
		{
			return this.Name.GetHashCode();
		}

		// Token: 0x06002B3D RID: 11069 RVA: 0x000A1464 File Offset: 0x0009F664
		public override EvidenceBase Clone()
		{
			return new Site(this.m_name);
		}

		// Token: 0x06002B3E RID: 11070 RVA: 0x000A1471 File Offset: 0x0009F671
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002B3F RID: 11071 RVA: 0x000A147C File Offset: 0x0009F67C
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.Site");
			securityElement.AddAttribute("version", "1");
			if (this.m_name != null)
			{
				securityElement.AddChild(new SecurityElement("Name", this.m_name.ToString()));
			}
			return securityElement;
		}

		// Token: 0x06002B40 RID: 11072 RVA: 0x000A14C8 File Offset: 0x0009F6C8
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002B41 RID: 11073 RVA: 0x000A14D5 File Offset: 0x0009F6D5
		internal object Normalize()
		{
			return this.m_name.ToString().ToUpper(CultureInfo.InvariantCulture);
		}

		// Token: 0x04001199 RID: 4505
		private SiteString m_name;
	}
}
