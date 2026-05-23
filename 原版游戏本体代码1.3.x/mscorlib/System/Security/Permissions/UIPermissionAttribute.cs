using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F9 RID: 761
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class UIPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x060026D6 RID: 9942 RVA: 0x0008CDD0 File Offset: 0x0008AFD0
		public UIPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004F3 RID: 1267
		// (get) Token: 0x060026D7 RID: 9943 RVA: 0x0008CDD9 File Offset: 0x0008AFD9
		// (set) Token: 0x060026D8 RID: 9944 RVA: 0x0008CDE1 File Offset: 0x0008AFE1
		public UIPermissionWindow Window
		{
			get
			{
				return this.m_windowFlag;
			}
			set
			{
				this.m_windowFlag = value;
			}
		}

		// Token: 0x170004F4 RID: 1268
		// (get) Token: 0x060026D9 RID: 9945 RVA: 0x0008CDEA File Offset: 0x0008AFEA
		// (set) Token: 0x060026DA RID: 9946 RVA: 0x0008CDF2 File Offset: 0x0008AFF2
		public UIPermissionClipboard Clipboard
		{
			get
			{
				return this.m_clipboardFlag;
			}
			set
			{
				this.m_clipboardFlag = value;
			}
		}

		// Token: 0x060026DB RID: 9947 RVA: 0x0008CDFB File Offset: 0x0008AFFB
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new UIPermission(PermissionState.Unrestricted);
			}
			return new UIPermission(this.m_windowFlag, this.m_clipboardFlag);
		}

		// Token: 0x04000F04 RID: 3844
		private UIPermissionWindow m_windowFlag;

		// Token: 0x04000F05 RID: 3845
		private UIPermissionClipboard m_clipboardFlag;
	}
}
