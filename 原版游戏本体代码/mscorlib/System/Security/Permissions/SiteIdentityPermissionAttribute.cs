using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002FC RID: 764
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class SiteIdentityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026E8 RID: 9960 RVA: 0x0008CF36 File Offset: 0x0008B136
		public SiteIdentityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004F9 RID: 1273
		// (get) Token: 0x060026E9 RID: 9961 RVA: 0x0008CF3F File Offset: 0x0008B13F
		// (set) Token: 0x060026EA RID: 9962 RVA: 0x0008CF47 File Offset: 0x0008B147
		public string Site
		{
			get
			{
				return this.m_site;
			}
			set
			{
				this.m_site = value;
			}
		}

		// Token: 0x060026EB RID: 9963 RVA: 0x0008CF50 File Offset: 0x0008B150
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new SiteIdentityPermission(PermissionState.Unrestricted);
			}
			if (this.m_site == null)
			{
				return new SiteIdentityPermission(PermissionState.None);
			}
			return new SiteIdentityPermission(this.m_site);
		}

		// Token: 0x04000F0A RID: 3850
		private string m_site;
	}
}
