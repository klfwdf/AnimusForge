using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002EF RID: 751
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public abstract class SecurityAttribute : Attribute
	{
		// Token: 0x06002658 RID: 9816 RVA: 0x0008C408 File Offset: 0x0008A608
		protected SecurityAttribute(SecurityAction action)
		{
			this.m_action = action;
		}

		// Token: 0x170004BE RID: 1214
		// (get) Token: 0x06002659 RID: 9817 RVA: 0x0008C417 File Offset: 0x0008A617
		// (set) Token: 0x0600265A RID: 9818 RVA: 0x0008C41F File Offset: 0x0008A61F
		public SecurityAction Action
		{
			get
			{
				return this.m_action;
			}
			set
			{
				this.m_action = value;
			}
		}

		// Token: 0x170004BF RID: 1215
		// (get) Token: 0x0600265B RID: 9819 RVA: 0x0008C428 File Offset: 0x0008A628
		// (set) Token: 0x0600265C RID: 9820 RVA: 0x0008C430 File Offset: 0x0008A630
		public bool Unrestricted
		{
			get
			{
				return this.m_unrestricted;
			}
			set
			{
				this.m_unrestricted = value;
			}
		}

		// Token: 0x0600265D RID: 9821
		public abstract IPermission CreatePermission();

		// Token: 0x0600265E RID: 9822 RVA: 0x0008C43C File Offset: 0x0008A63C
		[SecurityCritical]
		internal static IntPtr FindSecurityAttributeTypeHandle(string typeName)
		{
			PermissionSet.s_fullTrust.Assert();
			Type type = Type.GetType(typeName, false, false);
			if (type == null)
			{
				return IntPtr.Zero;
			}
			return type.TypeHandle.Value;
		}

		// Token: 0x04000EE7 RID: 3815
		internal SecurityAction m_action;

		// Token: 0x04000EE8 RID: 3816
		internal bool m_unrestricted;
	}
}
