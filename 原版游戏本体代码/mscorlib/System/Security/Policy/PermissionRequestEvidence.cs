using System;
using System.Runtime.InteropServices;

namespace System.Security.Policy
{
	// Token: 0x02000362 RID: 866
	[ComVisible(true)]
	[Obsolete("Assembly level declarative security is obsolete and is no longer enforced by the CLR by default. See http://go.microsoft.com/fwlink/?LinkID=155570 for more information.")]
	[Serializable]
	public sealed class PermissionRequestEvidence : EvidenceBase
	{
		// Token: 0x06002ACB RID: 10955 RVA: 0x0009E804 File Offset: 0x0009CA04
		public PermissionRequestEvidence(PermissionSet request, PermissionSet optional, PermissionSet denied)
		{
			if (request == null)
			{
				this.m_request = null;
			}
			else
			{
				this.m_request = request.Copy();
			}
			if (optional == null)
			{
				this.m_optional = null;
			}
			else
			{
				this.m_optional = optional.Copy();
			}
			if (denied == null)
			{
				this.m_denied = null;
				return;
			}
			this.m_denied = denied.Copy();
		}

		// Token: 0x170005B6 RID: 1462
		// (get) Token: 0x06002ACC RID: 10956 RVA: 0x0009E85E File Offset: 0x0009CA5E
		public PermissionSet RequestedPermissions
		{
			get
			{
				return this.m_request;
			}
		}

		// Token: 0x170005B7 RID: 1463
		// (get) Token: 0x06002ACD RID: 10957 RVA: 0x0009E866 File Offset: 0x0009CA66
		public PermissionSet OptionalPermissions
		{
			get
			{
				return this.m_optional;
			}
		}

		// Token: 0x170005B8 RID: 1464
		// (get) Token: 0x06002ACE RID: 10958 RVA: 0x0009E86E File Offset: 0x0009CA6E
		public PermissionSet DeniedPermissions
		{
			get
			{
				return this.m_denied;
			}
		}

		// Token: 0x06002ACF RID: 10959 RVA: 0x0009E876 File Offset: 0x0009CA76
		public override EvidenceBase Clone()
		{
			return this.Copy();
		}

		// Token: 0x06002AD0 RID: 10960 RVA: 0x0009E87E File Offset: 0x0009CA7E
		public PermissionRequestEvidence Copy()
		{
			return new PermissionRequestEvidence(this.m_request, this.m_optional, this.m_denied);
		}

		// Token: 0x06002AD1 RID: 10961 RVA: 0x0009E898 File Offset: 0x0009CA98
		internal SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("System.Security.Policy.PermissionRequestEvidence");
			securityElement.AddAttribute("version", "1");
			if (this.m_request != null)
			{
				SecurityElement securityElement2 = new SecurityElement("Request");
				securityElement2.AddChild(this.m_request.ToXml());
				securityElement.AddChild(securityElement2);
			}
			if (this.m_optional != null)
			{
				SecurityElement securityElement2 = new SecurityElement("Optional");
				securityElement2.AddChild(this.m_optional.ToXml());
				securityElement.AddChild(securityElement2);
			}
			if (this.m_denied != null)
			{
				SecurityElement securityElement2 = new SecurityElement("Denied");
				securityElement2.AddChild(this.m_denied.ToXml());
				securityElement.AddChild(securityElement2);
			}
			return securityElement;
		}

		// Token: 0x06002AD2 RID: 10962 RVA: 0x0009E942 File Offset: 0x0009CB42
		public override string ToString()
		{
			return this.ToXml().ToString();
		}

		// Token: 0x04001170 RID: 4464
		private PermissionSet m_request;

		// Token: 0x04001171 RID: 4465
		private PermissionSet m_optional;

		// Token: 0x04001172 RID: 4466
		private PermissionSet m_denied;

		// Token: 0x04001173 RID: 4467
		private string m_strRequest;

		// Token: 0x04001174 RID: 4468
		private string m_strOptional;

		// Token: 0x04001175 RID: 4469
		private string m_strDenied;
	}
}
