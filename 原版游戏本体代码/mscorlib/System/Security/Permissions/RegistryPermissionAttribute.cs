using System;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace System.Security.Permissions
{
	// Token: 0x020002F7 RID: 759
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class RegistryPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026A6 RID: 9894 RVA: 0x0008C9B1 File Offset: 0x0008ABB1
		public RegistryPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004DD RID: 1245
		// (get) Token: 0x060026A7 RID: 9895 RVA: 0x0008C9BA File Offset: 0x0008ABBA
		// (set) Token: 0x060026A8 RID: 9896 RVA: 0x0008C9C2 File Offset: 0x0008ABC2
		public string Read
		{
			get
			{
				return this.m_read;
			}
			set
			{
				this.m_read = value;
			}
		}

		// Token: 0x170004DE RID: 1246
		// (get) Token: 0x060026A9 RID: 9897 RVA: 0x0008C9CB File Offset: 0x0008ABCB
		// (set) Token: 0x060026AA RID: 9898 RVA: 0x0008C9D3 File Offset: 0x0008ABD3
		public string Write
		{
			get
			{
				return this.m_write;
			}
			set
			{
				this.m_write = value;
			}
		}

		// Token: 0x170004DF RID: 1247
		// (get) Token: 0x060026AB RID: 9899 RVA: 0x0008C9DC File Offset: 0x0008ABDC
		// (set) Token: 0x060026AC RID: 9900 RVA: 0x0008C9E4 File Offset: 0x0008ABE4
		public string Create
		{
			get
			{
				return this.m_create;
			}
			set
			{
				this.m_create = value;
			}
		}

		// Token: 0x170004E0 RID: 1248
		// (get) Token: 0x060026AD RID: 9901 RVA: 0x0008C9ED File Offset: 0x0008ABED
		// (set) Token: 0x060026AE RID: 9902 RVA: 0x0008C9F5 File Offset: 0x0008ABF5
		public string ViewAccessControl
		{
			get
			{
				return this.m_viewAcl;
			}
			set
			{
				this.m_viewAcl = value;
			}
		}

		// Token: 0x170004E1 RID: 1249
		// (get) Token: 0x060026AF RID: 9903 RVA: 0x0008C9FE File Offset: 0x0008ABFE
		// (set) Token: 0x060026B0 RID: 9904 RVA: 0x0008CA06 File Offset: 0x0008AC06
		public string ChangeAccessControl
		{
			get
			{
				return this.m_changeAcl;
			}
			set
			{
				this.m_changeAcl = value;
			}
		}

		// Token: 0x170004E2 RID: 1250
		// (get) Token: 0x060026B1 RID: 9905 RVA: 0x0008CA0F File Offset: 0x0008AC0F
		// (set) Token: 0x060026B2 RID: 9906 RVA: 0x0008CA20 File Offset: 0x0008AC20
		public string ViewAndModify
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GetMethod"));
			}
			set
			{
				this.m_read = value;
				this.m_write = value;
				this.m_create = value;
			}
		}

		// Token: 0x170004E3 RID: 1251
		// (get) Token: 0x060026B3 RID: 9907 RVA: 0x0008CA37 File Offset: 0x0008AC37
		// (set) Token: 0x060026B4 RID: 9908 RVA: 0x0008CA48 File Offset: 0x0008AC48
		[Obsolete("Please use the ViewAndModify property instead.")]
		public string All
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GetMethod"));
			}
			set
			{
				this.m_read = value;
				this.m_write = value;
				this.m_create = value;
			}
		}

		// Token: 0x060026B5 RID: 9909 RVA: 0x0008CA60 File Offset: 0x0008AC60
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new RegistryPermission(PermissionState.Unrestricted);
			}
			RegistryPermission registryPermission = new RegistryPermission(PermissionState.None);
			if (this.m_read != null)
			{
				registryPermission.SetPathList(RegistryPermissionAccess.Read, this.m_read);
			}
			if (this.m_write != null)
			{
				registryPermission.SetPathList(RegistryPermissionAccess.Write, this.m_write);
			}
			if (this.m_create != null)
			{
				registryPermission.SetPathList(RegistryPermissionAccess.Create, this.m_create);
			}
			if (this.m_viewAcl != null)
			{
				registryPermission.SetPathList(AccessControlActions.View, this.m_viewAcl);
			}
			if (this.m_changeAcl != null)
			{
				registryPermission.SetPathList(AccessControlActions.Change, this.m_changeAcl);
			}
			return registryPermission;
		}

		// Token: 0x04000EFE RID: 3838
		private string m_read;

		// Token: 0x04000EFF RID: 3839
		private string m_write;

		// Token: 0x04000F00 RID: 3840
		private string m_create;

		// Token: 0x04000F01 RID: 3841
		private string m_viewAcl;

		// Token: 0x04000F02 RID: 3842
		private string m_changeAcl;
	}
}
