using System;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.AccessControl;

namespace System.Security.Permissions
{
	// Token: 0x020002F3 RID: 755
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class FileIOPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x0600266E RID: 9838 RVA: 0x0008C599 File Offset: 0x0008A799
		public FileIOPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004C5 RID: 1221
		// (get) Token: 0x0600266F RID: 9839 RVA: 0x0008C5A2 File Offset: 0x0008A7A2
		// (set) Token: 0x06002670 RID: 9840 RVA: 0x0008C5AA File Offset: 0x0008A7AA
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

		// Token: 0x170004C6 RID: 1222
		// (get) Token: 0x06002671 RID: 9841 RVA: 0x0008C5B3 File Offset: 0x0008A7B3
		// (set) Token: 0x06002672 RID: 9842 RVA: 0x0008C5BB File Offset: 0x0008A7BB
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

		// Token: 0x170004C7 RID: 1223
		// (get) Token: 0x06002673 RID: 9843 RVA: 0x0008C5C4 File Offset: 0x0008A7C4
		// (set) Token: 0x06002674 RID: 9844 RVA: 0x0008C5CC File Offset: 0x0008A7CC
		public string Append
		{
			get
			{
				return this.m_append;
			}
			set
			{
				this.m_append = value;
			}
		}

		// Token: 0x170004C8 RID: 1224
		// (get) Token: 0x06002675 RID: 9845 RVA: 0x0008C5D5 File Offset: 0x0008A7D5
		// (set) Token: 0x06002676 RID: 9846 RVA: 0x0008C5DD File Offset: 0x0008A7DD
		public string PathDiscovery
		{
			get
			{
				return this.m_pathDiscovery;
			}
			set
			{
				this.m_pathDiscovery = value;
			}
		}

		// Token: 0x170004C9 RID: 1225
		// (get) Token: 0x06002677 RID: 9847 RVA: 0x0008C5E6 File Offset: 0x0008A7E6
		// (set) Token: 0x06002678 RID: 9848 RVA: 0x0008C5EE File Offset: 0x0008A7EE
		public string ViewAccessControl
		{
			get
			{
				return this.m_viewAccess;
			}
			set
			{
				this.m_viewAccess = value;
			}
		}

		// Token: 0x170004CA RID: 1226
		// (get) Token: 0x06002679 RID: 9849 RVA: 0x0008C5F7 File Offset: 0x0008A7F7
		// (set) Token: 0x0600267A RID: 9850 RVA: 0x0008C5FF File Offset: 0x0008A7FF
		public string ChangeAccessControl
		{
			get
			{
				return this.m_changeAccess;
			}
			set
			{
				this.m_changeAccess = value;
			}
		}

		// Token: 0x170004CB RID: 1227
		// (get) Token: 0x0600267C RID: 9852 RVA: 0x0008C626 File Offset: 0x0008A826
		// (set) Token: 0x0600267B RID: 9851 RVA: 0x0008C608 File Offset: 0x0008A808
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
				this.m_append = value;
				this.m_pathDiscovery = value;
			}
		}

		// Token: 0x170004CC RID: 1228
		// (get) Token: 0x0600267D RID: 9853 RVA: 0x0008C637 File Offset: 0x0008A837
		// (set) Token: 0x0600267E RID: 9854 RVA: 0x0008C648 File Offset: 0x0008A848
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
				this.m_append = value;
				this.m_pathDiscovery = value;
			}
		}

		// Token: 0x170004CD RID: 1229
		// (get) Token: 0x0600267F RID: 9855 RVA: 0x0008C666 File Offset: 0x0008A866
		// (set) Token: 0x06002680 RID: 9856 RVA: 0x0008C66E File Offset: 0x0008A86E
		public FileIOPermissionAccess AllFiles
		{
			get
			{
				return this.m_allFiles;
			}
			set
			{
				this.m_allFiles = value;
			}
		}

		// Token: 0x170004CE RID: 1230
		// (get) Token: 0x06002681 RID: 9857 RVA: 0x0008C677 File Offset: 0x0008A877
		// (set) Token: 0x06002682 RID: 9858 RVA: 0x0008C67F File Offset: 0x0008A87F
		public FileIOPermissionAccess AllLocalFiles
		{
			get
			{
				return this.m_allLocalFiles;
			}
			set
			{
				this.m_allLocalFiles = value;
			}
		}

		// Token: 0x06002683 RID: 9859 RVA: 0x0008C688 File Offset: 0x0008A888
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new FileIOPermission(PermissionState.Unrestricted);
			}
			FileIOPermission fileIOPermission = new FileIOPermission(PermissionState.None);
			if (this.m_read != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.Read, this.m_read);
			}
			if (this.m_write != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.Write, this.m_write);
			}
			if (this.m_append != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.Append, this.m_append);
			}
			if (this.m_pathDiscovery != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.PathDiscovery, this.m_pathDiscovery);
			}
			if (this.m_viewAccess != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.NoAccess, AccessControlActions.View, new string[] { this.m_viewAccess }, false);
			}
			if (this.m_changeAccess != null)
			{
				fileIOPermission.SetPathList(FileIOPermissionAccess.NoAccess, AccessControlActions.Change, new string[] { this.m_changeAccess }, false);
			}
			fileIOPermission.AllFiles = this.m_allFiles;
			fileIOPermission.AllLocalFiles = this.m_allLocalFiles;
			return fileIOPermission;
		}

		// Token: 0x04000EEC RID: 3820
		private string m_read;

		// Token: 0x04000EED RID: 3821
		private string m_write;

		// Token: 0x04000EEE RID: 3822
		private string m_append;

		// Token: 0x04000EEF RID: 3823
		private string m_pathDiscovery;

		// Token: 0x04000EF0 RID: 3824
		private string m_viewAccess;

		// Token: 0x04000EF1 RID: 3825
		private string m_changeAccess;

		// Token: 0x04000EF2 RID: 3826
		[OptionalField(VersionAdded = 2)]
		private FileIOPermissionAccess m_allLocalFiles;

		// Token: 0x04000EF3 RID: 3827
		[OptionalField(VersionAdded = 2)]
		private FileIOPermissionAccess m_allFiles;
	}
}
