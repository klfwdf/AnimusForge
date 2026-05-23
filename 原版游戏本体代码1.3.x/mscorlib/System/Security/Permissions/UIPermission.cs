using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x0200030E RID: 782
	[ComVisible(true)]
	[Serializable]
	public sealed class UIPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x06002774 RID: 10100 RVA: 0x0008F6F6 File Offset: 0x0008D8F6
		public UIPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.SetUnrestricted(true);
				return;
			}
			if (state == PermissionState.None)
			{
				this.SetUnrestricted(false);
				this.Reset();
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x06002775 RID: 10101 RVA: 0x0008F72A File Offset: 0x0008D92A
		public UIPermission(UIPermissionWindow windowFlag, UIPermissionClipboard clipboardFlag)
		{
			UIPermission.VerifyWindowFlag(windowFlag);
			UIPermission.VerifyClipboardFlag(clipboardFlag);
			this.m_windowFlag = windowFlag;
			this.m_clipboardFlag = clipboardFlag;
		}

		// Token: 0x06002776 RID: 10102 RVA: 0x0008F74C File Offset: 0x0008D94C
		public UIPermission(UIPermissionWindow windowFlag)
		{
			UIPermission.VerifyWindowFlag(windowFlag);
			this.m_windowFlag = windowFlag;
		}

		// Token: 0x06002777 RID: 10103 RVA: 0x0008F761 File Offset: 0x0008D961
		public UIPermission(UIPermissionClipboard clipboardFlag)
		{
			UIPermission.VerifyClipboardFlag(clipboardFlag);
			this.m_clipboardFlag = clipboardFlag;
		}

		// Token: 0x1700050C RID: 1292
		// (get) Token: 0x06002779 RID: 10105 RVA: 0x0008F785 File Offset: 0x0008D985
		// (set) Token: 0x06002778 RID: 10104 RVA: 0x0008F776 File Offset: 0x0008D976
		public UIPermissionWindow Window
		{
			get
			{
				return this.m_windowFlag;
			}
			set
			{
				UIPermission.VerifyWindowFlag(value);
				this.m_windowFlag = value;
			}
		}

		// Token: 0x1700050D RID: 1293
		// (get) Token: 0x0600277B RID: 10107 RVA: 0x0008F79C File Offset: 0x0008D99C
		// (set) Token: 0x0600277A RID: 10106 RVA: 0x0008F78D File Offset: 0x0008D98D
		public UIPermissionClipboard Clipboard
		{
			get
			{
				return this.m_clipboardFlag;
			}
			set
			{
				UIPermission.VerifyClipboardFlag(value);
				this.m_clipboardFlag = value;
			}
		}

		// Token: 0x0600277C RID: 10108 RVA: 0x0008F7A4 File Offset: 0x0008D9A4
		private static void VerifyWindowFlag(UIPermissionWindow flag)
		{
			if (flag < UIPermissionWindow.NoWindows || flag > UIPermissionWindow.AllWindows)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)flag }));
			}
		}

		// Token: 0x0600277D RID: 10109 RVA: 0x0008F7CD File Offset: 0x0008D9CD
		private static void VerifyClipboardFlag(UIPermissionClipboard flag)
		{
			if (flag < UIPermissionClipboard.NoClipboard || flag > UIPermissionClipboard.AllClipboard)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)flag }));
			}
		}

		// Token: 0x0600277E RID: 10110 RVA: 0x0008F7F6 File Offset: 0x0008D9F6
		private void Reset()
		{
			this.m_windowFlag = UIPermissionWindow.NoWindows;
			this.m_clipboardFlag = UIPermissionClipboard.NoClipboard;
		}

		// Token: 0x0600277F RID: 10111 RVA: 0x0008F806 File Offset: 0x0008DA06
		private void SetUnrestricted(bool unrestricted)
		{
			if (unrestricted)
			{
				this.m_windowFlag = UIPermissionWindow.AllWindows;
				this.m_clipboardFlag = UIPermissionClipboard.AllClipboard;
			}
		}

		// Token: 0x06002780 RID: 10112 RVA: 0x0008F819 File Offset: 0x0008DA19
		public bool IsUnrestricted()
		{
			return this.m_windowFlag == UIPermissionWindow.AllWindows && this.m_clipboardFlag == UIPermissionClipboard.AllClipboard;
		}

		// Token: 0x06002781 RID: 10113 RVA: 0x0008F830 File Offset: 0x0008DA30
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.m_windowFlag == UIPermissionWindow.NoWindows && this.m_clipboardFlag == UIPermissionClipboard.NoClipboard;
			}
			bool result;
			try
			{
				UIPermission uipermission = (UIPermission)target;
				if (uipermission.IsUnrestricted())
				{
					result = true;
				}
				else if (this.IsUnrestricted())
				{
					result = false;
				}
				else
				{
					result = this.m_windowFlag <= uipermission.m_windowFlag && this.m_clipboardFlag <= uipermission.m_clipboardFlag;
				}
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return result;
		}

		// Token: 0x06002782 RID: 10114 RVA: 0x0008F8D0 File Offset: 0x0008DAD0
		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			if (!base.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			UIPermission uipermission = (UIPermission)target;
			UIPermissionWindow uipermissionWindow = ((this.m_windowFlag < uipermission.m_windowFlag) ? this.m_windowFlag : uipermission.m_windowFlag);
			UIPermissionClipboard uipermissionClipboard = ((this.m_clipboardFlag < uipermission.m_clipboardFlag) ? this.m_clipboardFlag : uipermission.m_clipboardFlag);
			if (uipermissionWindow == UIPermissionWindow.NoWindows && uipermissionClipboard == UIPermissionClipboard.NoClipboard)
			{
				return null;
			}
			return new UIPermission(uipermissionWindow, uipermissionClipboard);
		}

		// Token: 0x06002783 RID: 10115 RVA: 0x0008F960 File Offset: 0x0008DB60
		public override IPermission Union(IPermission target)
		{
			if (target == null)
			{
				return this.Copy();
			}
			if (!base.VerifyType(target))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			UIPermission uipermission = (UIPermission)target;
			UIPermissionWindow uipermissionWindow = ((this.m_windowFlag > uipermission.m_windowFlag) ? this.m_windowFlag : uipermission.m_windowFlag);
			UIPermissionClipboard uipermissionClipboard = ((this.m_clipboardFlag > uipermission.m_clipboardFlag) ? this.m_clipboardFlag : uipermission.m_clipboardFlag);
			if (uipermissionWindow == UIPermissionWindow.NoWindows && uipermissionClipboard == UIPermissionClipboard.NoClipboard)
			{
				return null;
			}
			return new UIPermission(uipermissionWindow, uipermissionClipboard);
		}

		// Token: 0x06002784 RID: 10116 RVA: 0x0008F9F4 File Offset: 0x0008DBF4
		public override IPermission Copy()
		{
			return new UIPermission(this.m_windowFlag, this.m_clipboardFlag);
		}

		// Token: 0x06002785 RID: 10117 RVA: 0x0008FA08 File Offset: 0x0008DC08
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.UIPermission");
			if (!this.IsUnrestricted())
			{
				if (this.m_windowFlag != UIPermissionWindow.NoWindows)
				{
					securityElement.AddAttribute("Window", Enum.GetName(typeof(UIPermissionWindow), this.m_windowFlag));
				}
				if (this.m_clipboardFlag != UIPermissionClipboard.NoClipboard)
				{
					securityElement.AddAttribute("Clipboard", Enum.GetName(typeof(UIPermissionClipboard), this.m_clipboardFlag));
				}
			}
			else
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			return securityElement;
		}

		// Token: 0x06002786 RID: 10118 RVA: 0x0008FA98 File Offset: 0x0008DC98
		public override void FromXml(SecurityElement esd)
		{
			CodeAccessPermission.ValidateElement(esd, this);
			if (XMLUtil.IsUnrestricted(esd))
			{
				this.SetUnrestricted(true);
				return;
			}
			this.m_windowFlag = UIPermissionWindow.NoWindows;
			this.m_clipboardFlag = UIPermissionClipboard.NoClipboard;
			string text = esd.Attribute("Window");
			if (text != null)
			{
				this.m_windowFlag = (UIPermissionWindow)Enum.Parse(typeof(UIPermissionWindow), text);
			}
			string text2 = esd.Attribute("Clipboard");
			if (text2 != null)
			{
				this.m_clipboardFlag = (UIPermissionClipboard)Enum.Parse(typeof(UIPermissionClipboard), text2);
			}
		}

		// Token: 0x06002787 RID: 10119 RVA: 0x0008FB1E File Offset: 0x0008DD1E
		int IBuiltInPermission.GetTokenIndex()
		{
			return UIPermission.GetTokenIndex();
		}

		// Token: 0x06002788 RID: 10120 RVA: 0x0008FB25 File Offset: 0x0008DD25
		internal static int GetTokenIndex()
		{
			return 7;
		}

		// Token: 0x04000F54 RID: 3924
		private UIPermissionWindow m_windowFlag;

		// Token: 0x04000F55 RID: 3925
		private UIPermissionClipboard m_clipboardFlag;
	}
}
