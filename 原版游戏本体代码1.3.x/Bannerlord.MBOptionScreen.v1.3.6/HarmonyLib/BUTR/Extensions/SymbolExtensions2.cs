using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x0200006A RID: 106
	[NullableContext(2)]
	[Nullable(0)]
	internal static class SymbolExtensions2
	{
		// Token: 0x06000435 RID: 1077 RVA: 0x00010430 File Offset: 0x0000E630
		public static ConstructorInfo GetConstructorInfo<TResult>([Nullable(1)] Expression<Func<TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x0001044C File Offset: 0x0000E64C
		public static ConstructorInfo GetConstructorInfo<T1, TResult>([Nullable(1)] Expression<Func<T1, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x00010468 File Offset: 0x0000E668
		public static ConstructorInfo GetConstructorInfo<T1, T2, TResult>([Nullable(1)] Expression<Func<T1, T2, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x00010484 File Offset: 0x0000E684
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x06000439 RID: 1081 RVA: 0x000104A0 File Offset: 0x0000E6A0
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043A RID: 1082 RVA: 0x000104BC File Offset: 0x0000E6BC
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043B RID: 1083 RVA: 0x000104D8 File Offset: 0x0000E6D8
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043C RID: 1084 RVA: 0x000104F4 File Offset: 0x0000E6F4
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043D RID: 1085 RVA: 0x00010510 File Offset: 0x0000E710
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043E RID: 1086 RVA: 0x0001052C File Offset: 0x0000E72C
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetConstructorInfo(expression);
			}
			return null;
		}

		// Token: 0x0600043F RID: 1087 RVA: 0x00010548 File Offset: 0x0000E748
		[NullableContext(1)]
		[return: Nullable(2)]
		public static ConstructorInfo GetConstructorInfo(LambdaExpression expression)
		{
			NewExpression body = ((expression != null) ? expression.Body : null) as NewExpression;
			if (body != null && body.Constructor != null)
			{
				return body.Constructor;
			}
			return null;
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x0001057C File Offset: 0x0000E77C
		public static FieldInfo GetFieldInfo<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetFieldInfo(expression);
			}
			return null;
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x00010598 File Offset: 0x0000E798
		public static FieldInfo GetFieldInfo<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetFieldInfo(expression);
			}
			return null;
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x000105B4 File Offset: 0x0000E7B4
		[NullableContext(1)]
		[return: Nullable(2)]
		public static FieldInfo GetFieldInfo(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					return fieldInfo;
				}
			}
			return null;
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x000105E8 File Offset: 0x0000E7E8
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> GetFieldRefAccess<[Nullable(2)] TField>(Expression<Func<TField>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetFieldRefAccess<TField>(expression);
			}
			return null;
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00010604 File Offset: 0x0000E804
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> GetFieldRefAccess<[Nullable(2)] TField>(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					if (!(fieldInfo == null))
					{
						return AccessTools2.FieldRefAccess<object, TField>(fieldInfo, true);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x0001064C File Offset: 0x0000E84C
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<TObject, TField> GetFieldRefAccess<TObject, [Nullable(2)] TField>(Expression<Func<TObject, TField>> expression) where TObject : class
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetFieldRefAccess<TObject, TField>(expression);
			}
			return null;
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00010668 File Offset: 0x0000E868
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<TObject, TField> GetFieldRefAccess<TObject, [Nullable(2)] TField>(LambdaExpression expression) where TObject : class
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					if (!(fieldInfo == null))
					{
						return AccessTools2.FieldRefAccess<TObject, TField>(fieldInfo, true);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x000106B0 File Offset: 0x0000E8B0
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetMethodInfo(Expression<Action> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x000106CC File Offset: 0x0000E8CC
		public static MethodInfo GetMethodInfo<T1>([Nullable(1)] Expression<Action<T1>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x000106E8 File Offset: 0x0000E8E8
		public static MethodInfo GetMethodInfo<T1, T2>([Nullable(1)] Expression<Action<T1, T2>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x00010704 File Offset: 0x0000E904
		public static MethodInfo GetMethodInfo<T1, T2, T3>([Nullable(1)] Expression<Action<T1, T2, T3>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x00010720 File Offset: 0x0000E920
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4>([Nullable(1)] Expression<Action<T1, T2, T3, T4>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044C RID: 1100 RVA: 0x0001073C File Offset: 0x0000E93C
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044D RID: 1101 RVA: 0x00010758 File Offset: 0x0000E958
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044E RID: 1102 RVA: 0x00010774 File Offset: 0x0000E974
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600044F RID: 1103 RVA: 0x00010790 File Offset: 0x0000E990
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000450 RID: 1104 RVA: 0x000107AC File Offset: 0x0000E9AC
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000451 RID: 1105 RVA: 0x000107C8 File Offset: 0x0000E9C8
		public static MethodInfo GetMethodInfo<TResult>([Nullable(1)] Expression<Func<TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000452 RID: 1106 RVA: 0x000107E4 File Offset: 0x0000E9E4
		public static MethodInfo GetMethodInfo<T1, TResult>([Nullable(1)] Expression<Func<T1, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000453 RID: 1107 RVA: 0x00010800 File Offset: 0x0000EA00
		public static MethodInfo GetMethodInfo<T1, T2, TResult>([Nullable(1)] Expression<Func<T1, T2, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000454 RID: 1108 RVA: 0x0001081C File Offset: 0x0000EA1C
		public static MethodInfo GetMethodInfo<T1, T2, T3, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000455 RID: 1109 RVA: 0x00010838 File Offset: 0x0000EA38
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000456 RID: 1110 RVA: 0x00010854 File Offset: 0x0000EA54
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000457 RID: 1111 RVA: 0x00010870 File Offset: 0x0000EA70
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000458 RID: 1112 RVA: 0x0001088C File Offset: 0x0000EA8C
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x06000459 RID: 1113 RVA: 0x000108A8 File Offset: 0x0000EAA8
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600045A RID: 1114 RVA: 0x000108C4 File Offset: 0x0000EAC4
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetMethodInfo(expression);
			}
			return null;
		}

		// Token: 0x0600045B RID: 1115 RVA: 0x000108E0 File Offset: 0x0000EAE0
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetMethodInfo(LambdaExpression expression)
		{
			MethodCallExpression methodCallExpression = ((expression != null) ? expression.Body : null) as MethodCallExpression;
			if (methodCallExpression != null)
			{
				MethodInfo methodInfo = methodCallExpression.Method;
				if (methodInfo != null)
				{
					return methodInfo;
				}
			}
			return null;
		}

		// Token: 0x0600045C RID: 1116 RVA: 0x00010910 File Offset: 0x0000EB10
		public static PropertyInfo GetPropertyInfo<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertyInfo(expression);
			}
			return null;
		}

		// Token: 0x0600045D RID: 1117 RVA: 0x0001092C File Offset: 0x0000EB2C
		public static PropertyInfo GetPropertyInfo<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertyInfo(expression);
			}
			return null;
		}

		// Token: 0x0600045E RID: 1118 RVA: 0x00010948 File Offset: 0x0000EB48
		[NullableContext(1)]
		[return: Nullable(2)]
		public static PropertyInfo GetPropertyInfo(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
				if (propertyInfo != null)
				{
					return propertyInfo;
				}
			}
			return null;
		}

		// Token: 0x0600045F RID: 1119 RVA: 0x0001097C File Offset: 0x0000EB7C
		public static MethodInfo GetPropertyGetter<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertyGetter(expression);
			}
			return null;
		}

		// Token: 0x06000460 RID: 1120 RVA: 0x00010998 File Offset: 0x0000EB98
		public static MethodInfo GetPropertyGetter<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertyGetter(expression);
			}
			return null;
		}

		// Token: 0x06000461 RID: 1121 RVA: 0x000109B4 File Offset: 0x0000EBB4
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetPropertyGetter(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
				if (propertyInfo != null)
				{
					if (propertyInfo == null)
					{
						return null;
					}
					return propertyInfo.GetGetMethod(true);
				}
			}
			return null;
		}

		// Token: 0x06000462 RID: 1122 RVA: 0x000109F4 File Offset: 0x0000EBF4
		public static MethodInfo GetPropertySetter<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertySetter(expression);
			}
			return null;
		}

		// Token: 0x06000463 RID: 1123 RVA: 0x00010A10 File Offset: 0x0000EC10
		public static MethodInfo GetPropertySetter<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetPropertySetter(expression);
			}
			return null;
		}

		// Token: 0x06000464 RID: 1124 RVA: 0x00010A2C File Offset: 0x0000EC2C
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetPropertySetter(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				PropertyInfo propertyInfo = memberExpression.Member as PropertyInfo;
				if (propertyInfo != null)
				{
					if (propertyInfo == null)
					{
						return null;
					}
					return propertyInfo.GetSetMethod(true);
				}
			}
			return null;
		}

		// Token: 0x06000465 RID: 1125 RVA: 0x00010A6C File Offset: 0x0000EC6C
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> GetStaticFieldRefAccess<[Nullable(2)] TField>(Expression<Func<TField>> expression)
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetStaticFieldRefAccess<TField>(expression);
			}
			return null;
		}

		// Token: 0x06000466 RID: 1126 RVA: 0x00010A88 File Offset: 0x0000EC88
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> GetStaticFieldRefAccess<[Nullable(2)] TField>(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					if (!(fieldInfo == null))
					{
						return AccessTools2.StaticFieldRefAccess<TField>(fieldInfo, true);
					}
					return null;
				}
			}
			return null;
		}

		// Token: 0x06000467 RID: 1127 RVA: 0x00010AD0 File Offset: 0x0000ECD0
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<TObject, TField> GetStructFieldRefAccess<TObject, [Nullable(2)] TField>([Nullable(1)] Expression<Func<TField>> expression) where TObject : struct
		{
			if (expression != null)
			{
				return SymbolExtensions2.GetStructFieldRefAccess<TObject, TField>(expression);
			}
			return null;
		}

		// Token: 0x06000468 RID: 1128 RVA: 0x00010AEC File Offset: 0x0000ECEC
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<TObject, TField> GetStructFieldRefAccess<TObject, [Nullable(2)] TField>([Nullable(1)] LambdaExpression expression) where TObject : struct
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			if (memberExpression != null)
			{
				FieldInfo fieldInfo = memberExpression.Member as FieldInfo;
				if (fieldInfo != null)
				{
					if (!(fieldInfo == null))
					{
						return AccessTools2.StructFieldRefAccess<TObject, TField>(fieldInfo, true);
					}
					return null;
				}
			}
			return null;
		}
	}
}
