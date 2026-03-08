using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F5 RID: 757
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class PrincipalPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x06002692 RID: 9874 RVA: 0x0008C864 File Offset: 0x0008AA64
		public PrincipalPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004D5 RID: 1237
		// (get) Token: 0x06002693 RID: 9875 RVA: 0x0008C874 File Offset: 0x0008AA74
		// (set) Token: 0x06002694 RID: 9876 RVA: 0x0008C87C File Offset: 0x0008AA7C
		public string Name
		{
			get
			{
				return this.m_name;
			}
			set
			{
				this.m_name = value;
			}
		}

		// Token: 0x170004D6 RID: 1238
		// (get) Token: 0x06002695 RID: 9877 RVA: 0x0008C885 File Offset: 0x0008AA85
		// (set) Token: 0x06002696 RID: 9878 RVA: 0x0008C88D File Offset: 0x0008AA8D
		public string Role
		{
			get
			{
				return this.m_role;
			}
			set
			{
				this.m_role = value;
			}
		}

		// Token: 0x170004D7 RID: 1239
		// (get) Token: 0x06002697 RID: 9879 RVA: 0x0008C896 File Offset: 0x0008AA96
		// (set) Token: 0x06002698 RID: 9880 RVA: 0x0008C89E File Offset: 0x0008AA9E
		public bool Authenticated
		{
			get
			{
				return this.m_authenticated;
			}
			set
			{
				this.m_authenticated = value;
			}
		}

		// Token: 0x06002699 RID: 9881 RVA: 0x0008C8A7 File Offset: 0x0008AAA7
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new PrincipalPermission(PermissionState.Unrestricted);
			}
			return new PrincipalPermission(this.m_name, this.m_role, this.m_authenticated);
		}

		// Token: 0x04000EFA RID: 3834
		private string m_name;

		// Token: 0x04000EFB RID: 3835
		private string m_role;

		// Token: 0x04000EFC RID: 3836
		private bool m_authenticated = true;
	}
}
