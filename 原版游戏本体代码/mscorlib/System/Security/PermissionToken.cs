using System;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;
using System.Security.Util;

namespace System.Security
{
	// Token: 0x020001E1 RID: 481
	[Serializable]
	internal sealed class PermissionToken : ISecurityEncodable
	{
		// Token: 0x06001D27 RID: 7463 RVA: 0x000650AC File Offset: 0x000632AC
		internal static bool IsMscorlibClassName(string className)
		{
			int num = className.IndexOf(',');
			if (num == -1)
			{
				return true;
			}
			num = className.LastIndexOf(']');
			if (num == -1)
			{
				num = 0;
			}
			for (int i = num; i < className.Length; i++)
			{
				if ((className[i] == 'm' || className[i] == 'M') && string.Compare(className, i, "mscorlib", 0, "mscorlib".Length, StringComparison.OrdinalIgnoreCase) == 0)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06001D28 RID: 7464 RVA: 0x0006511B File Offset: 0x0006331B
		static PermissionToken()
		{
			PermissionToken.s_theTokenFactory = new PermissionTokenFactory(4);
		}

		// Token: 0x06001D29 RID: 7465 RVA: 0x0006513A File Offset: 0x0006333A
		internal PermissionToken()
		{
		}

		// Token: 0x06001D2A RID: 7466 RVA: 0x00065142 File Offset: 0x00063342
		internal PermissionToken(int index, PermissionTokenType type, string strTypeName)
		{
			this.m_index = index;
			this.m_type = type;
			this.m_strTypeName = strTypeName;
		}

		// Token: 0x06001D2B RID: 7467 RVA: 0x00065164 File Offset: 0x00063364
		[SecurityCritical]
		public static PermissionToken GetToken(Type cls)
		{
			if (cls == null)
			{
				return null;
			}
			if (cls.GetInterface("System.Security.Permissions.IBuiltInPermission") != null)
			{
				if (PermissionToken.s_reflectPerm == null)
				{
					PermissionToken.s_reflectPerm = new ReflectionPermission(PermissionState.Unrestricted);
				}
				PermissionToken.s_reflectPerm.Assert();
				MethodInfo method = cls.GetMethod("GetTokenIndex", BindingFlags.Static | BindingFlags.NonPublic);
				RuntimeMethodInfo runtimeMethodInfo = method as RuntimeMethodInfo;
				int index = (int)runtimeMethodInfo.UnsafeInvoke(null, BindingFlags.Default, null, null, null);
				return PermissionToken.s_theTokenFactory.BuiltInGetToken(index, null, cls);
			}
			return PermissionToken.s_theTokenFactory.GetToken(cls, null);
		}

		// Token: 0x06001D2C RID: 7468 RVA: 0x000651F4 File Offset: 0x000633F4
		public static PermissionToken GetToken(IPermission perm)
		{
			if (perm == null)
			{
				return null;
			}
			IBuiltInPermission builtInPermission = perm as IBuiltInPermission;
			if (builtInPermission != null)
			{
				return PermissionToken.s_theTokenFactory.BuiltInGetToken(builtInPermission.GetTokenIndex(), perm, null);
			}
			return PermissionToken.s_theTokenFactory.GetToken(perm.GetType(), perm);
		}

		// Token: 0x06001D2D RID: 7469 RVA: 0x00065234 File Offset: 0x00063434
		public static PermissionToken GetToken(string typeStr)
		{
			return PermissionToken.GetToken(typeStr, false);
		}

		// Token: 0x06001D2E RID: 7470 RVA: 0x00065240 File Offset: 0x00063440
		public static PermissionToken GetToken(string typeStr, bool bCreateMscorlib)
		{
			if (typeStr == null)
			{
				return null;
			}
			if (!PermissionToken.IsMscorlibClassName(typeStr))
			{
				return PermissionToken.s_theTokenFactory.GetToken(typeStr);
			}
			if (!bCreateMscorlib)
			{
				return null;
			}
			return PermissionToken.FindToken(Type.GetType(typeStr));
		}

		// Token: 0x06001D2F RID: 7471 RVA: 0x00065278 File Offset: 0x00063478
		[SecuritySafeCritical]
		public static PermissionToken FindToken(Type cls)
		{
			if (cls == null)
			{
				return null;
			}
			if (cls.GetInterface("System.Security.Permissions.IBuiltInPermission") != null)
			{
				if (PermissionToken.s_reflectPerm == null)
				{
					PermissionToken.s_reflectPerm = new ReflectionPermission(PermissionState.Unrestricted);
				}
				PermissionToken.s_reflectPerm.Assert();
				MethodInfo method = cls.GetMethod("GetTokenIndex", BindingFlags.Static | BindingFlags.NonPublic);
				RuntimeMethodInfo runtimeMethodInfo = method as RuntimeMethodInfo;
				int index = (int)runtimeMethodInfo.UnsafeInvoke(null, BindingFlags.Default, null, null, null);
				return PermissionToken.s_theTokenFactory.BuiltInGetToken(index, null, cls);
			}
			return PermissionToken.s_theTokenFactory.FindToken(cls);
		}

		// Token: 0x06001D30 RID: 7472 RVA: 0x00065304 File Offset: 0x00063504
		public static PermissionToken FindTokenByIndex(int i)
		{
			return PermissionToken.s_theTokenFactory.FindTokenByIndex(i);
		}

		// Token: 0x06001D31 RID: 7473 RVA: 0x00065314 File Offset: 0x00063514
		public static bool IsTokenProperlyAssigned(IPermission perm, PermissionToken token)
		{
			PermissionToken token2 = PermissionToken.GetToken(perm);
			return token2.m_index == token.m_index && token.m_type == token2.m_type && (!(perm.GetType().Module.Assembly == Assembly.GetExecutingAssembly()) || token2.m_index < 17);
		}

		// Token: 0x06001D32 RID: 7474 RVA: 0x00065378 File Offset: 0x00063578
		public SecurityElement ToXml()
		{
			SecurityElement securityElement = new SecurityElement("PermissionToken");
			if ((this.m_type & PermissionTokenType.BuiltIn) != (PermissionTokenType)0)
			{
				securityElement.AddAttribute("Index", this.m_index.ToString() ?? "");
			}
			else
			{
				securityElement.AddAttribute("Name", SecurityElement.Escape(this.m_strTypeName));
			}
			securityElement.AddAttribute("Type", this.m_type.ToString("F"));
			return securityElement;
		}

		// Token: 0x06001D33 RID: 7475 RVA: 0x000653F8 File Offset: 0x000635F8
		public void FromXml(SecurityElement elRoot)
		{
			elRoot.Tag.Equals("PermissionToken");
			string text = elRoot.Attribute("Name");
			PermissionToken permissionToken;
			if (text != null)
			{
				permissionToken = PermissionToken.GetToken(text, true);
			}
			else
			{
				permissionToken = PermissionToken.FindTokenByIndex(int.Parse(elRoot.Attribute("Index"), CultureInfo.InvariantCulture));
			}
			this.m_index = permissionToken.m_index;
			this.m_type = (PermissionTokenType)Enum.Parse(typeof(PermissionTokenType), elRoot.Attribute("Type"));
			this.m_strTypeName = permissionToken.m_strTypeName;
		}

		// Token: 0x04000A2E RID: 2606
		private static readonly PermissionTokenFactory s_theTokenFactory;

		// Token: 0x04000A2F RID: 2607
		private static volatile ReflectionPermission s_reflectPerm = null;

		// Token: 0x04000A30 RID: 2608
		private const string c_mscorlibName = "mscorlib";

		// Token: 0x04000A31 RID: 2609
		internal int m_index;

		// Token: 0x04000A32 RID: 2610
		internal volatile PermissionTokenType m_type;

		// Token: 0x04000A33 RID: 2611
		internal string m_strTypeName;

		// Token: 0x04000A34 RID: 2612
		internal static TokenBasedSet s_tokenSet = new TokenBasedSet();
	}
}
