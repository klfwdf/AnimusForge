using System;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x020002E6 RID: 742
	[Serializable]
	internal sealed class HostProtectionPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x06002634 RID: 9780 RVA: 0x0008BAD1 File Offset: 0x00089CD1
		public HostProtectionPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.Resources = HostProtectionResource.All;
				return;
			}
			if (state == PermissionState.None)
			{
				this.Resources = HostProtectionResource.None;
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x06002635 RID: 9781 RVA: 0x0008BB03 File Offset: 0x00089D03
		public HostProtectionPermission(HostProtectionResource resources)
		{
			this.Resources = resources;
		}

		// Token: 0x06002636 RID: 9782 RVA: 0x0008BB12 File Offset: 0x00089D12
		public bool IsUnrestricted()
		{
			return this.Resources == HostProtectionResource.All;
		}

		// Token: 0x170004BB RID: 1211
		// (get) Token: 0x06002638 RID: 9784 RVA: 0x0008BB55 File Offset: 0x00089D55
		// (set) Token: 0x06002637 RID: 9783 RVA: 0x0008BB21 File Offset: 0x00089D21
		public HostProtectionResource Resources
		{
			get
			{
				return this.m_resources;
			}
			set
			{
				if (value < HostProtectionResource.None || value > HostProtectionResource.All)
				{
					throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)value }));
				}
				this.m_resources = value;
			}
		}

		// Token: 0x06002639 RID: 9785 RVA: 0x0008BB60 File Offset: 0x00089D60
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.m_resources == HostProtectionResource.None;
			}
			if (base.GetType() != target.GetType())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return (this.m_resources & ((HostProtectionPermission)target).m_resources) == this.m_resources;
		}

		// Token: 0x0600263A RID: 9786 RVA: 0x0008BBCC File Offset: 0x00089DCC
		public override IPermission Union(IPermission target)
		{
			if (target == null)
			{
				return this.Copy();
			}
			if (base.GetType() != target.GetType())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			HostProtectionResource resources = this.m_resources | ((HostProtectionPermission)target).m_resources;
			return new HostProtectionPermission(resources);
		}

		// Token: 0x0600263B RID: 9787 RVA: 0x0008BC34 File Offset: 0x00089E34
		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			if (base.GetType() != target.GetType())
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			HostProtectionResource hostProtectionResource = this.m_resources & ((HostProtectionPermission)target).m_resources;
			if (hostProtectionResource == HostProtectionResource.None)
			{
				return null;
			}
			return new HostProtectionPermission(hostProtectionResource);
		}

		// Token: 0x0600263C RID: 9788 RVA: 0x0008BC9B File Offset: 0x00089E9B
		public override IPermission Copy()
		{
			return new HostProtectionPermission(this.m_resources);
		}

		// Token: 0x0600263D RID: 9789 RVA: 0x0008BCA8 File Offset: 0x00089EA8
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, base.GetType().FullName);
			if (this.IsUnrestricted())
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			else
			{
				securityElement.AddAttribute("Resources", XMLUtil.BitFieldEnumToString(typeof(HostProtectionResource), this.Resources));
			}
			return securityElement;
		}

		// Token: 0x0600263E RID: 9790 RVA: 0x0008BD08 File Offset: 0x00089F08
		public override void FromXml(SecurityElement esd)
		{
			CodeAccessPermission.ValidateElement(esd, this);
			if (XMLUtil.IsUnrestricted(esd))
			{
				this.Resources = HostProtectionResource.All;
				return;
			}
			string text = esd.Attribute("Resources");
			if (text == null)
			{
				this.Resources = HostProtectionResource.None;
				return;
			}
			this.Resources = (HostProtectionResource)Enum.Parse(typeof(HostProtectionResource), text);
		}

		// Token: 0x0600263F RID: 9791 RVA: 0x0008BD62 File Offset: 0x00089F62
		int IBuiltInPermission.GetTokenIndex()
		{
			return HostProtectionPermission.GetTokenIndex();
		}

		// Token: 0x06002640 RID: 9792 RVA: 0x0008BD69 File Offset: 0x00089F69
		internal static int GetTokenIndex()
		{
			return 9;
		}

		// Token: 0x04000E9F RID: 3743
		internal static volatile HostProtectionResource protectedResources;

		// Token: 0x04000EA0 RID: 3744
		private HostProtectionResource m_resources;
	}
}
