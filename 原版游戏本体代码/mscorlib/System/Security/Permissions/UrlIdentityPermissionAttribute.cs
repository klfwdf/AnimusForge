using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002FD RID: 765
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class UrlIdentityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026EC RID: 9964 RVA: 0x0008CF7B File Offset: 0x0008B17B
		public UrlIdentityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004FA RID: 1274
		// (get) Token: 0x060026ED RID: 9965 RVA: 0x0008CF84 File Offset: 0x0008B184
		// (set) Token: 0x060026EE RID: 9966 RVA: 0x0008CF8C File Offset: 0x0008B18C
		public string Url
		{
			get
			{
				return this.m_url;
			}
			set
			{
				this.m_url = value;
			}
		}

		// Token: 0x060026EF RID: 9967 RVA: 0x0008CF95 File Offset: 0x0008B195
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new UrlIdentityPermission(PermissionState.Unrestricted);
			}
			if (this.m_url == null)
			{
				return new UrlIdentityPermission(PermissionState.None);
			}
			return new UrlIdentityPermission(this.m_url);
		}

		// Token: 0x04000F0B RID: 3851
		private string m_url;
	}
}
