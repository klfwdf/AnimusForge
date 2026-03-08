using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F2 RID: 754
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class FileDialogPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x06002668 RID: 9832 RVA: 0x0008C51E File Offset: 0x0008A71E
		public FileDialogPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004C3 RID: 1219
		// (get) Token: 0x06002669 RID: 9833 RVA: 0x0008C527 File Offset: 0x0008A727
		// (set) Token: 0x0600266A RID: 9834 RVA: 0x0008C534 File Offset: 0x0008A734
		public bool Open
		{
			get
			{
				return (this.m_access & FileDialogPermissionAccess.Open) > FileDialogPermissionAccess.None;
			}
			set
			{
				this.m_access = (value ? (this.m_access | FileDialogPermissionAccess.Open) : (this.m_access & ~FileDialogPermissionAccess.Open));
			}
		}

		// Token: 0x170004C4 RID: 1220
		// (get) Token: 0x0600266B RID: 9835 RVA: 0x0008C552 File Offset: 0x0008A752
		// (set) Token: 0x0600266C RID: 9836 RVA: 0x0008C55F File Offset: 0x0008A75F
		public bool Save
		{
			get
			{
				return (this.m_access & FileDialogPermissionAccess.Save) > FileDialogPermissionAccess.None;
			}
			set
			{
				this.m_access = (value ? (this.m_access | FileDialogPermissionAccess.Save) : (this.m_access & ~FileDialogPermissionAccess.Save));
			}
		}

		// Token: 0x0600266D RID: 9837 RVA: 0x0008C57D File Offset: 0x0008A77D
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new FileDialogPermission(PermissionState.Unrestricted);
			}
			return new FileDialogPermission(this.m_access);
		}

		// Token: 0x04000EEB RID: 3819
		private FileDialogPermissionAccess m_access;
	}
}
