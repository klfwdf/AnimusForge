using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F1 RID: 753
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class EnvironmentPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x06002660 RID: 9824 RVA: 0x0008C484 File Offset: 0x0008A684
		public EnvironmentPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004C0 RID: 1216
		// (get) Token: 0x06002661 RID: 9825 RVA: 0x0008C48D File Offset: 0x0008A68D
		// (set) Token: 0x06002662 RID: 9826 RVA: 0x0008C495 File Offset: 0x0008A695
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

		// Token: 0x170004C1 RID: 1217
		// (get) Token: 0x06002663 RID: 9827 RVA: 0x0008C49E File Offset: 0x0008A69E
		// (set) Token: 0x06002664 RID: 9828 RVA: 0x0008C4A6 File Offset: 0x0008A6A6
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

		// Token: 0x170004C2 RID: 1218
		// (get) Token: 0x06002665 RID: 9829 RVA: 0x0008C4AF File Offset: 0x0008A6AF
		// (set) Token: 0x06002666 RID: 9830 RVA: 0x0008C4C0 File Offset: 0x0008A6C0
		public string All
		{
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("NotSupported_GetMethod"));
			}
			set
			{
				this.m_write = value;
				this.m_read = value;
			}
		}

		// Token: 0x06002667 RID: 9831 RVA: 0x0008C4D0 File Offset: 0x0008A6D0
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new EnvironmentPermission(PermissionState.Unrestricted);
			}
			EnvironmentPermission environmentPermission = new EnvironmentPermission(PermissionState.None);
			if (this.m_read != null)
			{
				environmentPermission.SetPathList(EnvironmentPermissionAccess.Read, this.m_read);
			}
			if (this.m_write != null)
			{
				environmentPermission.SetPathList(EnvironmentPermissionAccess.Write, this.m_write);
			}
			return environmentPermission;
		}

		// Token: 0x04000EE9 RID: 3817
		private string m_read;

		// Token: 0x04000EEA RID: 3818
		private string m_write;
	}
}
