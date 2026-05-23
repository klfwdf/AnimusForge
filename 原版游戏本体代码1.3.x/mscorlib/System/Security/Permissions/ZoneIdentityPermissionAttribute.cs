using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002FA RID: 762
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class ZoneIdentityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026DC RID: 9948 RVA: 0x0008CE1D File Offset: 0x0008B01D
		public ZoneIdentityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004F5 RID: 1269
		// (get) Token: 0x060026DD RID: 9949 RVA: 0x0008CE2D File Offset: 0x0008B02D
		// (set) Token: 0x060026DE RID: 9950 RVA: 0x0008CE35 File Offset: 0x0008B035
		public SecurityZone Zone
		{
			get
			{
				return this.m_flag;
			}
			set
			{
				this.m_flag = value;
			}
		}

		// Token: 0x060026DF RID: 9951 RVA: 0x0008CE3E File Offset: 0x0008B03E
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new ZoneIdentityPermission(PermissionState.Unrestricted);
			}
			return new ZoneIdentityPermission(this.m_flag);
		}

		// Token: 0x04000F06 RID: 3846
		private SecurityZone m_flag = SecurityZone.NoZone;
	}
}
