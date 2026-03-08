using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Newtonsoft.Json.Utilities
{
	// Token: 0x02000070 RID: 112
	[NullableContext(1)]
	[Nullable(0)]
	internal static class TypeExtensions
	{
		// Token: 0x060005FE RID: 1534 RVA: 0x000199C8 File Offset: 0x00017BC8
		public static MethodInfo Method(this Delegate d)
		{
			return d.Method;
		}

		// Token: 0x060005FF RID: 1535 RVA: 0x000199D0 File Offset: 0x00017BD0
		public static MemberTypes MemberType(this MemberInfo memberInfo)
		{
			return memberInfo.MemberType;
		}

		// Token: 0x06000600 RID: 1536 RVA: 0x000199D8 File Offset: 0x00017BD8
		public static bool ContainsGenericParameters(this Type type)
		{
			return type.ContainsGenericParameters;
		}

		// Token: 0x06000601 RID: 1537 RVA: 0x000199E0 File Offset: 0x00017BE0
		public static bool IsInterface(this Type type)
		{
			return type.IsInterface;
		}

		// Token: 0x06000602 RID: 1538 RVA: 0x000199E8 File Offset: 0x00017BE8
		public static bool IsGenericType(this Type type)
		{
			return type.IsGenericType;
		}

		// Token: 0x06000603 RID: 1539 RVA: 0x000199F0 File Offset: 0x00017BF0
		public static bool IsGenericTypeDefinition(this Type type)
		{
			return type.IsGenericTypeDefinition;
		}

		// Token: 0x06000604 RID: 1540 RVA: 0x000199F8 File Offset: 0x00017BF8
		[return: Nullable(2)]
		public static Type BaseType(this Type type)
		{
			return type.BaseType;
		}

		// Token: 0x06000605 RID: 1541 RVA: 0x00019A00 File Offset: 0x00017C00
		public static Assembly Assembly(this Type type)
		{
			return type.Assembly;
		}

		// Token: 0x06000606 RID: 1542 RVA: 0x00019A08 File Offset: 0x00017C08
		public static bool IsEnum(this Type type)
		{
			return type.IsEnum;
		}

		// Token: 0x06000607 RID: 1543 RVA: 0x00019A10 File Offset: 0x00017C10
		public static bool IsClass(this Type type)
		{
			return type.IsClass;
		}

		// Token: 0x06000608 RID: 1544 RVA: 0x00019A18 File Offset: 0x00017C18
		public static bool IsSealed(this Type type)
		{
			return type.IsSealed;
		}

		// Token: 0x06000609 RID: 1545 RVA: 0x00019A20 File Offset: 0x00017C20
		public static bool IsAbstract(this Type type)
		{
			return type.IsAbstract;
		}

		// Token: 0x0600060A RID: 1546 RVA: 0x00019A28 File Offset: 0x00017C28
		public static bool IsVisible(this Type type)
		{
			return type.IsVisible;
		}

		// Token: 0x0600060B RID: 1547 RVA: 0x00019A30 File Offset: 0x00017C30
		public static bool IsValueType(this Type type)
		{
			return type.IsValueType;
		}

		// Token: 0x0600060C RID: 1548 RVA: 0x00019A38 File Offset: 0x00017C38
		public static bool IsPrimitive(this Type type)
		{
			return type.IsPrimitive;
		}

		// Token: 0x0600060D RID: 1549 RVA: 0x00019A40 File Offset: 0x00017C40
		public static bool AssignableToTypeName(this Type type, string fullTypeName, bool searchInterfaces, [Nullable(2)] [NotNullWhen(true)] out Type match)
		{
			Type type2 = type;
			while (type2 != null)
			{
				if (string.Equals(type2.FullName, fullTypeName, StringComparison.Ordinal))
				{
					match = type2;
					return true;
				}
				type2 = type2.BaseType();
			}
			if (searchInterfaces)
			{
				Type[] interfaces = type.GetInterfaces();
				for (int i = 0; i < interfaces.Length; i++)
				{
					if (string.Equals(interfaces[i].Name, fullTypeName, StringComparison.Ordinal))
					{
						match = type;
						return true;
					}
				}
			}
			match = null;
			return false;
		}

		// Token: 0x0600060E RID: 1550 RVA: 0x00019AA8 File Offset: 0x00017CA8
		public static bool AssignableToTypeName(this Type type, string fullTypeName, bool searchInterfaces)
		{
			Type type2;
			return type.AssignableToTypeName(fullTypeName, searchInterfaces, out type2);
		}

		// Token: 0x0600060F RID: 1551 RVA: 0x00019AC0 File Offset: 0x00017CC0
		public static bool ImplementInterface(this Type type, Type interfaceType)
		{
			Type type2 = type;
			while (type2 != null)
			{
				foreach (Type type3 in ((IEnumerable<Type>)type2.GetInterfaces()))
				{
					if (type3 == interfaceType || (type3 != null && type3.ImplementInterface(interfaceType)))
					{
						return true;
					}
				}
				type2 = type2.BaseType();
			}
			return false;
		}
	}
}
