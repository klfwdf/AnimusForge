using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000311 RID: 785
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class GacIdentityPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060027AC RID: 10156 RVA: 0x000906F7 File Offset: 0x0008E8F7
		public GacIdentityPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x060027AD RID: 10157 RVA: 0x00090700 File Offset: 0x0008E900
		public override IPermission CreatePermission()
		{
			return new GacIdentityPermission();
		}
	}
}
