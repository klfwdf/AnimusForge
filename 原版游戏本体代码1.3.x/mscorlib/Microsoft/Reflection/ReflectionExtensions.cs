using System;
using System.Reflection;

namespace Microsoft.Reflection
{
	// Token: 0x02000030 RID: 48
	internal static class ReflectionExtensions
	{
		// Token: 0x060001DC RID: 476 RVA: 0x00004DA6 File Offset: 0x00002FA6
		public static bool IsEnum(this Type type)
		{
			return type.IsEnum;
		}

		// Token: 0x060001DD RID: 477 RVA: 0x00004DAE File Offset: 0x00002FAE
		public static bool IsAbstract(this Type type)
		{
			return type.IsAbstract;
		}

		// Token: 0x060001DE RID: 478 RVA: 0x00004DB6 File Offset: 0x00002FB6
		public static bool IsSealed(this Type type)
		{
			return type.IsSealed;
		}

		// Token: 0x060001DF RID: 479 RVA: 0x00004DBE File Offset: 0x00002FBE
		public static Type BaseType(this Type type)
		{
			return type.BaseType;
		}

		// Token: 0x060001E0 RID: 480 RVA: 0x00004DC6 File Offset: 0x00002FC6
		public static Assembly Assembly(this Type type)
		{
			return type.Assembly;
		}

		// Token: 0x060001E1 RID: 481 RVA: 0x00004DCE File Offset: 0x00002FCE
		public static TypeCode GetTypeCode(this Type type)
		{
			return Type.GetTypeCode(type);
		}

		// Token: 0x060001E2 RID: 482 RVA: 0x00004DD6 File Offset: 0x00002FD6
		public static bool ReflectionOnly(this Assembly assm)
		{
			return assm.ReflectionOnly;
		}
	}
}
