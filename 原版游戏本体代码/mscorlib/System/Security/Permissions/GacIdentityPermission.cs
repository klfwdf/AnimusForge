using System;
using System.Runtime.InteropServices;

namespace System.Security.Permissions
{
	// Token: 0x02000312 RID: 786
	[ComVisible(true)]
	[Serializable]
	public sealed class GacIdentityPermission : CodeAccessPermission, IBuiltInPermission
	{
		// Token: 0x060027AE RID: 10158 RVA: 0x00090707 File Offset: 0x0008E907
		public GacIdentityPermission(PermissionState state)
		{
			if (state != PermissionState.Unrestricted && state != PermissionState.None)
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_InvalidPermissionState"));
			}
		}

		// Token: 0x060027AF RID: 10159 RVA: 0x00090726 File Offset: 0x0008E926
		public GacIdentityPermission()
		{
		}

		// Token: 0x060027B0 RID: 10160 RVA: 0x0009072E File Offset: 0x0008E92E
		public override IPermission Copy()
		{
			return new GacIdentityPermission();
		}

		// Token: 0x060027B1 RID: 10161 RVA: 0x00090735 File Offset: 0x0008E935
		public override bool IsSubsetOf(IPermission target)
		{
			if (target == null)
			{
				return false;
			}
			if (!(target is GacIdentityPermission))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return true;
		}

		// Token: 0x060027B2 RID: 10162 RVA: 0x00090769 File Offset: 0x0008E969
		public override IPermission Intersect(IPermission target)
		{
			if (target == null)
			{
				return null;
			}
			if (!(target is GacIdentityPermission))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return this.Copy();
		}

		// Token: 0x060027B3 RID: 10163 RVA: 0x000907A2 File Offset: 0x0008E9A2
		public override IPermission Union(IPermission target)
		{
			if (target == null)
			{
				return this.Copy();
			}
			if (!(target is GacIdentityPermission))
			{
				throw new ArgumentException(Environment.GetResourceString("Argument_WrongType", new object[] { base.GetType().FullName }));
			}
			return this.Copy();
		}

		// Token: 0x060027B4 RID: 10164 RVA: 0x000907E0 File Offset: 0x0008E9E0
		public override SecurityElement ToXml()
		{
			return CodeAccessPermission.CreatePermissionElement(this, "System.Security.Permissions.GacIdentityPermission");
		}

		// Token: 0x060027B5 RID: 10165 RVA: 0x000907FA File Offset: 0x0008E9FA
		public override void FromXml(SecurityElement securityElement)
		{
			CodeAccessPermission.ValidateElement(securityElement, this);
		}

		// Token: 0x060027B6 RID: 10166 RVA: 0x00090803 File Offset: 0x0008EA03
		int IBuiltInPermission.GetTokenIndex()
		{
			return GacIdentityPermission.GetTokenIndex();
		}

		// Token: 0x060027B7 RID: 10167 RVA: 0x0009080A File Offset: 0x0008EA0A
		internal static int GetTokenIndex()
		{
			return 15;
		}
	}
}
