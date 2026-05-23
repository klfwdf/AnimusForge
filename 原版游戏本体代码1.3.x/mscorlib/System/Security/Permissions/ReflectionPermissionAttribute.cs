using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x020002F6 RID: 758
	[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Constructor | AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	[ComVisible(true)]
	[Serializable]
	public sealed class ReflectionPermissionAttribute : CodeAccessSecurityAttribute
	{
		// Token: 0x0600269A RID: 9882 RVA: 0x0008C8CF File Offset: 0x0008AACF
		public ReflectionPermissionAttribute(SecurityAction action)
			: base(action)
		{
		}

		// Token: 0x170004D8 RID: 1240
		// (get) Token: 0x0600269B RID: 9883 RVA: 0x0008C8D8 File Offset: 0x0008AAD8
		// (set) Token: 0x0600269C RID: 9884 RVA: 0x0008C8E0 File Offset: 0x0008AAE0
		public ReflectionPermissionFlag Flags
		{
			get
			{
				return this.m_flag;
			}
			set
			{
				this.m_flag = value;
			}
		}

		// Token: 0x170004D9 RID: 1241
		// (get) Token: 0x0600269D RID: 9885 RVA: 0x0008C8E9 File Offset: 0x0008AAE9
		// (set) Token: 0x0600269E RID: 9886 RVA: 0x0008C8F6 File Offset: 0x0008AAF6
		[Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
		public bool TypeInformation
		{
			get
			{
				return (this.m_flag & ReflectionPermissionFlag.TypeInformation) > ReflectionPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | ReflectionPermissionFlag.TypeInformation) : (this.m_flag & ~ReflectionPermissionFlag.TypeInformation));
			}
		}

		// Token: 0x170004DA RID: 1242
		// (get) Token: 0x0600269F RID: 9887 RVA: 0x0008C914 File Offset: 0x0008AB14
		// (set) Token: 0x060026A0 RID: 9888 RVA: 0x0008C921 File Offset: 0x0008AB21
		public bool MemberAccess
		{
			get
			{
				return (this.m_flag & ReflectionPermissionFlag.MemberAccess) > ReflectionPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | ReflectionPermissionFlag.MemberAccess) : (this.m_flag & ~ReflectionPermissionFlag.MemberAccess));
			}
		}

		// Token: 0x170004DB RID: 1243
		// (get) Token: 0x060026A1 RID: 9889 RVA: 0x0008C93F File Offset: 0x0008AB3F
		// (set) Token: 0x060026A2 RID: 9890 RVA: 0x0008C94C File Offset: 0x0008AB4C
		[Obsolete("This permission is no longer used by the CLR.")]
		public bool ReflectionEmit
		{
			get
			{
				return (this.m_flag & ReflectionPermissionFlag.ReflectionEmit) > ReflectionPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | ReflectionPermissionFlag.ReflectionEmit) : (this.m_flag & ~ReflectionPermissionFlag.ReflectionEmit));
			}
		}

		// Token: 0x170004DC RID: 1244
		// (get) Token: 0x060026A3 RID: 9891 RVA: 0x0008C96A File Offset: 0x0008AB6A
		// (set) Token: 0x060026A4 RID: 9892 RVA: 0x0008C977 File Offset: 0x0008AB77
		public bool RestrictedMemberAccess
		{
			get
			{
				return (this.m_flag & ReflectionPermissionFlag.RestrictedMemberAccess) > ReflectionPermissionFlag.NoFlags;
			}
			set
			{
				this.m_flag = (value ? (this.m_flag | ReflectionPermissionFlag.RestrictedMemberAccess) : (this.m_flag & ~ReflectionPermissionFlag.RestrictedMemberAccess));
			}
		}

		// Token: 0x060026A5 RID: 9893 RVA: 0x0008C995 File Offset: 0x0008AB95
		public override IPermission CreatePermission()
		{
			if (this.m_unrestricted)
			{
				return new ReflectionPermission(PermissionState.Unrestricted);
			}
			return new ReflectionPermission(this.m_flag);
		}

		// Token: 0x04000EFD RID: 3837
		private ReflectionPermissionFlag m_flag;
	}
}
