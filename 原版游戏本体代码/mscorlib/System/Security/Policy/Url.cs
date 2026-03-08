using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security.Policy
{
	// Token: 0x0200036F RID: 879
	[ComVisible(true)]
	[Serializable]
	public sealed class Url : EvidenceBase, IIdentityPermissionFactory
	{
		// Token: 0x06002B82 RID: 11138 RVA: 0x000A257E File Offset: 0x000A077E
		internal Url(string name, bool parsed)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.m_url = new URLString(name, parsed);
		}

		// Token: 0x06002B83 RID: 11139 RVA: 0x000A25A1 File Offset: 0x000A07A1
		public Url(string name)
		{
			if (name == null)
			{
				throw new ArgumentNullException("name");
			}
			this.m_url = new URLString(name);
		}

		// Token: 0x06002B84 RID: 11140 RVA: 0x000A25C3 File Offset: 0x000A07C3
		private Url(Url url)
		{
			this.m_url = url.m_url;
		}

		// Token: 0x170005D2 RID: 1490
		// (get) Token: 0x06002B85 RID: 11141 RVA: 0x000A25D7 File Offset: 0x000A07D7
		public string Value
		{
			get
			{
				return this.m_url.ToString();
			}
		}

		// Token: 0x06002B86 RID: 11142 RVA: 0x000A25E4 File Offset: 0x000A07E4
		internal URLString GetURLString()
		{
			return this.m_url;
		}

		// Token: 0x06002B87 RID: 11143 RVA: 0x000A25EC File Offset: 0x000A07EC
		public IPermission CreateIdentityPermission(Evidence evidence)
		{
			return new UrlIdentityPermission(this.m_url);
		}

		// Token: 0x06002B88 RID: 11144 RVA: 0x000A25FC File Offset: 0x000A07FC
		public override bool Equals(object o)
		{
			Url url = o as Url;
			return url != null && url.m_url.Equals(this.m_url);
		}

		// Token: 0x06002B89 RID: 11145 RVA: 0x000A2626 File Offset: 0x000A0826
		public override int GetHashCode()
		{
			return this.m_url.GetHashCode();
		}

		// Token: 0x06002B8A RID: 11146 RVA: 0x000A2633 File Offset: 0x000A0833
		public override EvidenceBase Clone()
		{
			return new Url(this);
		}

		// Token: 0x06002B8B RID: 11147 RVA: 0x000A263B File Offset: 0x000A083B
		public object Copy()
		{
			return this.Clone();
		}

		// Token: 0x06002B8C RID: 11148 RVA: 0x000A2644 File Offset: 0x000A0844
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.Url");
			securityElement.AddAttribute("version", "1");
			if (this.m_url != null)
			{
				securityElement.AddChild(new SecurityElement("Url", this.m_url.ToString()));
			}
			return securityElement;
		}

		// Token: 0x06002B8D RID: 11149 RVA: 0x000A2690 File Offset: 0x000A0890
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x06002B8E RID: 11150 RVA: 0x000A269D File Offset: 0x000A089D
		internal object Normalize()
		{
			return this.m_url.NormalizeUrl();
		}

		// Token: 0x040011A8 RID: 4520
		private URLString m_url;
	}
}
