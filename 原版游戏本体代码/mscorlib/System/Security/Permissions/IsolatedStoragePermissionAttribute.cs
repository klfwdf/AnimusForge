using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002FF RID: 767
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public abstract class IsolatedStoragePermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026F8 RID: 9976 RVA: 0x0008D086 File Offset: 0x0008B286
		protected IsolatedStoragePermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004FE RID: 1278
		// (get) Token: 0x060026FA RID: 9978 RVA: 0x0008D098 File Offset: 0x0008B298
		// (set) Token: 0x060026F9 RID: 9977 RVA: 0x0008D08F File Offset: 0x0008B28F
		public long UserQuota
		{
			get
			{
				return this.m_userQuota;
			}
			set
			{
				this.m_userQuota = value;
			}
		}

		// Token: 0x170004FF RID: 1279
		// (get) Token: 0x060026FC RID: 9980 RVA: 0x0008D0A9 File Offset: 0x0008B2A9
		// (set) Token: 0x060026FB RID: 9979 RVA: 0x0008D0A0 File Offset: 0x0008B2A0
		public IsolatedStorageContainment UsageAllowed
		{
			get
			{
				return this.m_allowed;
			}
			set
			{
				this.m_allowed = value;
			}
		}

		// Token: 0x04000F0F RID: 3855
		internal long m_userQuota;

		// Token: 0x04000F10 RID: 3856
		internal IsolatedStorageContainment m_allowed;
	}
}
