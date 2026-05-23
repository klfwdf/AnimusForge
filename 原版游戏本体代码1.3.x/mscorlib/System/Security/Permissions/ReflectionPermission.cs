using System;
using System.Runtime.InteropServices;
using System.Security.Util;

namespace System.Security.Permissions
{
	// Token: 0x02000303 RID: 771
	[ComVisible(true)]
	[Serializable]
	public sealed class ReflectionPermission : CodeAccessPermission, IUnrestrictedPermission, IBuiltInPermission
	{
		// Token: 0x0600270E RID: 9998 RVA: 0x0008D29C File Offset: 0x0008B49C
		public ReflectionPermission(PermissionState state)
		{
			if (state == PermissionState.Unrestricted)
			{
				this.SetUnrestricted(true);
				return;
			}
			if (state == PermissionState.None)
			{
				this.SetUnrestricted(false);
				return;
			}
			throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
		}

		// Token: 0x0600270F RID: 9999 RVA: 0x0008D2CA File Offset: 0x0008B4CA
		public ReflectionPermission(ReflectionPermissionFlag flag)
		{
			this.VerifyAccess(flag);
			this.SetUnrestricted(false);
			this.m_flags = flag;
		}

		// Token: 0x06002710 RID: 10000 RVA: 0x0008D2E7 File Offset: 0x0008B4E7
		private void SetUnrestricted(bool unrestricted)
		{
			if (unrestricted)
			{
				this.m_flags = ReflectionPermissionFlag.TypeInformation | ReflectionPermissionFlag.MemberAccess | ReflectionPermissionFlag.ReflectionEmit | ReflectionPermissionFlag.RestrictedMemberAccess;
				return;
			}
			this.Reset();
		}

		// Token: 0x06002711 RID: 10001 RVA: 0x0008D2FB File Offset: 0x0008B4FB
		private void Reset()
		{
			this.m_flags = ReflectionPermissionFlag.NoFlags;
		}

		// Token: 0x17000505 RID: 1285
		// (get) Token: 0x06002713 RID: 10003 RVA: 0x0008D314 File Offset: 0x0008B514
		// (set) Token: 0x06002712 RID: 10002 RVA: 0x0008D304 File Offset: 0x0008B504
		public ReflectionPermissionFlag Flags
		{
			get
			{
				return this.m_flags;
			}
			set
			{
				this.VerifyAccess(value);
				this.m_flags = value;
			}
		}

		// Token: 0x06002714 RID: 10004 RVA: 0x0008D31C File Offset: 0x0008B51C
		public bool IsUnrestricted()
		{
			return this.m_flags == (ReflectionPermissionFlag.TypeInformation | ReflectionPermissionFlag.MemberAccess | ReflectionPermissionFlag.ReflectionEmit | ReflectionPermissionFlag.RestrictedMemberAccess);
		}

		// Token: 0x06002715 RID: 10005 RVA: 0x0008D328 File Offset: 0x0008B528
		public override IPermission Union(IPermission other)
		{
			if (other == null)
			{
				return this.Copy();
			}
			if (!base.VerifyType(other))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			ReflectionPermission reflectionPermission = (ReflectionPermission)other;
			if (this.IsUnrestricted() || reflectionPermission.IsUnrestricted())
			{
				return new ReflectionPermission(PermissionState.Unrestricted);
			}
			ReflectionPermissionFlag flag = this.m_flags | reflectionPermission.m_flags;
			return new ReflectionPermission(flag);
		}

		// Token: 0x06002716 RID: 10006 RVA: 0x0008D3A0 File Offset: 0x0008B5A0
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return this.m_flags == ReflectionPermissionFlag.NoFlags;
			}
			bool result;
			try
			{
				ReflectionPermission reflectionPermission = (ReflectionPermission)target;
				if (reflectionPermission.IsUnrestricted())
				{
					result = true;
				}
				else if (this.IsUnrestricted())
				{
					result = false;
				}
				else
				{
					result = (this.m_flags & ~reflectionPermission.m_flags) == ReflectionPermissionFlag.NoFlags;
				}
			}
			catch (InvalidCastException)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return result;
		}

		// Token: 0x06002717 RID: 10007 RVA: 0x0008D424 File Offset: 0x0008B624
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
			ReflectionPermission reflectionPermission = (ReflectionPermission)target;
			ReflectionPermissionFlag reflectionPermissionFlag = reflectionPermission.m_flags & this.m_flags;
			if (reflectionPermissionFlag == ReflectionPermissionFlag.NoFlags)
			{
				return null;
			}
			return new ReflectionPermission(reflectionPermissionFlag);
		}

		// Token: 0x06002718 RID: 10008 RVA: 0x0008D483 File Offset: 0x0008B683
		public override IPermission Copy()
		{
			if (this.IsUnrestricted())
			{
				return new ReflectionPermission(PermissionState.Unrestricted);
			}
			return new ReflectionPermission(this.m_flags);
		}

		// Token: 0x06002719 RID: 10009 RVA: 0x0008D49F File Offset: 0x0008B69F
		private void VerifyAccess(ReflectionPermissionFlag type)
		{
			if ((type & ~(ReflectionPermissionFlag.TypeInformation | ReflectionPermissionFlag.MemberAccess | ReflectionPermissionFlag.ReflectionEmit | ReflectionPermissionFlag.RestrictedMemberAccess)) != ReflectionPermissionFlag.NoFlags)
			{
				throw new ArgumentException(Environment.GetResourceString("Arg_EnumIllegalVal", new object[] { (int)type }));
			}
		}

		// Token: 0x0600271A RID: 10010 RVA: 0x0008D4C8 File Offset: 0x0008B6C8
		public override SecurityElement ToXml()
		{
			SecurityElement securityElement = CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.ReflectionPermission");
			if (!this.IsUnrestricted())
			{
				securityElement.AddAttribute("Flags", XMLUtil.BitFieldEnumToString(typeof(ReflectionPermissionFlag), this.m_flags));
			}
			else
			{
				securityElement.AddAttribute("Unrestricted", "true");
			}
			return securityElement;
		}

		// Token: 0x0600271B RID: 10011 RVA: 0x0008D524 File Offset: 0x0008B724
		public override void FromXml(SecurityElement esd)
		{
			CodeAccessPermission.ValidateElement(esd, this);
			if (XMLUtil.IsUnrestricted(esd))
			{
				this.m_flags = ReflectionPermissionFlag.TypeInformation | ReflectionPermissionFlag.MemberAccess | ReflectionPermissionFlag.ReflectionEmit | ReflectionPermissionFlag.RestrictedMemberAccess;
				return;
			}
			this.Reset();
			this.SetUnrestricted(false);
			string text = esd.Attribute("Flags");
			if (text != null)
			{
				this.m_flags = (ReflectionPermissionFlag)Enum.Parse(typeof(ReflectionPermissionFlag), text);
			}
		}

		// Token: 0x0600271C RID: 10012 RVA: 0x0008D580 File Offset: 0x0008B780
		int IBuiltInPermission.GetTokenIndex()
		{
			return ReflectionPermission.GetTokenIndex();
		}

		// Token: 0x0600271D RID: 10013 RVA: 0x0008D587 File Offset: 0x0008B787
		internal static int GetTokenIndex()
		{
			return 4;
		}

		// Token: 0x04000F1D RID: 3869
		internal const ReflectionPermissionFlag AllFlagsAndMore = ReflectionPermissionFlag.TypeInformation | ReflectionPermissionFlag.MemberAccess | ReflectionPermissionFlag.ReflectionEmit | ReflectionPermissionFlag.RestrictedMemberAccess;

		// Token: 0x04000F1E RID: 3870
		private ReflectionPermissionFlag m_flags;
	}
}
