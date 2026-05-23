using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000040 RID: 64
	[NullableContext(2)]
	[Nullable(0)]
	internal static class SymbolExtensions2
	{
		// Token: 0x060003A3 RID: 931 RVA: 0x0000E830 File Offset: 0x0000CA30
		public static ConstructorInfo GetConstructorInfo<TResult>([Nullable(1)] Expression<Func<TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x0000E858 File Offset: 0x0000CA58
		public static ConstructorInfo GetConstructorInfo<T1, TResult>([Nullable(1)] Expression<Func<T1, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x0000E880 File Offset: 0x0000CA80
		public static ConstructorInfo GetConstructorInfo<T1, T2, TResult>([Nullable(1)] Expression<Func<T1, T2, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x0000E8A8 File Offset: 0x0000CAA8
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x0000E8D0 File Offset: 0x0000CAD0
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0000E8F8 File Offset: 0x0000CAF8
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0000E920 File Offset: 0x0000CB20
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AA RID: 938 RVA: 0x0000E948 File Offset: 0x0000CB48
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AB RID: 939 RVA: 0x0000E970 File Offset: 0x0000CB70
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AC RID: 940 RVA: 0x0000E998 File Offset: 0x0000CB98
		public static ConstructorInfo GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
		{
			bool flag = expression != null;
			ConstructorInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetConstructorInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AD RID: 941 RVA: 0x0000E9C0 File Offset: 0x0000CBC0
		[NullableContext(1)]
		[return: Nullable(2)]
		public static ConstructorInfo GetConstructorInfo(LambdaExpression expression)
		{
			NewExpression body = ((expression != null) ? expression.Body : null) as NewExpression;
			bool flag = body != null && body.Constructor != null;
			ConstructorInfo result;
			if (flag)
			{
				result = body.Constructor;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AE RID: 942 RVA: 0x0000EA04 File Offset: 0x0000CC04
		public static FieldInfo GetFieldInfo<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			bool flag = expression != null;
			FieldInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetFieldInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003AF RID: 943 RVA: 0x0000EA2C File Offset: 0x0000CC2C
		public static FieldInfo GetFieldInfo<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			bool flag = expression != null;
			FieldInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetFieldInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B0 RID: 944 RVA: 0x0000EA54 File Offset: 0x0000CC54
		[NullableContext(1)]
		[return: Nullable(2)]
		public static FieldInfo GetFieldInfo(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			FieldInfo fieldInfo;
			bool flag;
			if (memberExpression != null)
			{
				fieldInfo = memberExpression.Member as FieldInfo;
				flag = fieldInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			FieldInfo result;
			if (flag2)
			{
				result = fieldInfo;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B1 RID: 945 RVA: 0x0000EA98 File Offset: 0x0000CC98
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> GetFieldRefAccess<[Nullable(2)] TField>(Expression<Func<TField>> expression)
		{
			bool flag = expression != null;
			AccessTools.FieldRef<object, TField> result;
			if (flag)
			{
				result = SymbolExtensions2.GetFieldRefAccess<TField>(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B2 RID: 946 RVA: 0x0000EAC0 File Offset: 0x0000CCC0
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<object, TField> GetFieldRefAccess<[Nullable(2)] TField>(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			FieldInfo fieldInfo;
			bool flag;
			if (memberExpression != null)
			{
				fieldInfo = memberExpression.Member as FieldInfo;
				flag = fieldInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			AccessTools.FieldRef<object, TField> result;
			if (flag2)
			{
				result = ((fieldInfo == null) ? null : AccessTools2.FieldRefAccess<object, TField>(fieldInfo, true));
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B3 RID: 947 RVA: 0x0000EB18 File Offset: 0x0000CD18
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<TObject, TField> GetFieldRefAccess<TObject, [Nullable(2)] TField>(Expression<Func<TObject, TField>> expression) where TObject : class
		{
			bool flag = expression != null;
			AccessTools.FieldRef<TObject, TField> result;
			if (flag)
			{
				result = SymbolExtensions2.GetFieldRefAccess<TObject, TField>(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B4 RID: 948 RVA: 0x0000EB40 File Offset: 0x0000CD40
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1, 1 })]
		public static AccessTools.FieldRef<TObject, TField> GetFieldRefAccess<TObject, [Nullable(2)] TField>(LambdaExpression expression) where TObject : class
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			FieldInfo fieldInfo;
			bool flag;
			if (memberExpression != null)
			{
				fieldInfo = memberExpression.Member as FieldInfo;
				flag = fieldInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			AccessTools.FieldRef<TObject, TField> result;
			if (flag2)
			{
				result = ((fieldInfo == null) ? null : AccessTools2.FieldRefAccess<TObject, TField>(fieldInfo, true));
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B5 RID: 949 RVA: 0x0000EB98 File Offset: 0x0000CD98
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetMethodInfo(Expression<Action> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B6 RID: 950 RVA: 0x0000EBC0 File Offset: 0x0000CDC0
		public static MethodInfo GetMethodInfo<T1>([Nullable(1)] Expression<Action<T1>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B7 RID: 951 RVA: 0x0000EBE8 File Offset: 0x0000CDE8
		public static MethodInfo GetMethodInfo<T1, T2>([Nullable(1)] Expression<Action<T1, T2>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B8 RID: 952 RVA: 0x0000EC10 File Offset: 0x0000CE10
		public static MethodInfo GetMethodInfo<T1, T2, T3>([Nullable(1)] Expression<Action<T1, T2, T3>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003B9 RID: 953 RVA: 0x0000EC38 File Offset: 0x0000CE38
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4>([Nullable(1)] Expression<Action<T1, T2, T3, T4>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BA RID: 954 RVA: 0x0000EC60 File Offset: 0x0000CE60
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BB RID: 955 RVA: 0x0000EC88 File Offset: 0x0000CE88
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BC RID: 956 RVA: 0x0000ECB0 File Offset: 0x0000CEB0
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BD RID: 957 RVA: 0x0000ECD8 File Offset: 0x0000CED8
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BE RID: 958 RVA: 0x0000ED00 File Offset: 0x0000CF00
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003BF RID: 959 RVA: 0x0000ED28 File Offset: 0x0000CF28
		public static MethodInfo GetMethodInfo<TResult>([Nullable(1)] Expression<Func<TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C0 RID: 960 RVA: 0x0000ED50 File Offset: 0x0000CF50
		public static MethodInfo GetMethodInfo<T1, TResult>([Nullable(1)] Expression<Func<T1, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C1 RID: 961 RVA: 0x0000ED78 File Offset: 0x0000CF78
		public static MethodInfo GetMethodInfo<T1, T2, TResult>([Nullable(1)] Expression<Func<T1, T2, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C2 RID: 962 RVA: 0x0000EDA0 File Offset: 0x0000CFA0
		public static MethodInfo GetMethodInfo<T1, T2, T3, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C3 RID: 963 RVA: 0x0000EDC8 File Offset: 0x0000CFC8
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C4 RID: 964 RVA: 0x0000EDF0 File Offset: 0x0000CFF0
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C5 RID: 965 RVA: 0x0000EE18 File Offset: 0x0000D018
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C6 RID: 966 RVA: 0x0000EE40 File Offset: 0x0000D040
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C7 RID: 967 RVA: 0x0000EE68 File Offset: 0x0000D068
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>([Nullable(1)] Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C8 RID: 968 RVA: 0x0000EE90 File Offset: 0x0000D090
		public static MethodInfo GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>([Nullable(1)] Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetMethodInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003C9 RID: 969 RVA: 0x0000EEB8 File Offset: 0x0000D0B8
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetMethodInfo(LambdaExpression expression)
		{
			MethodCallExpression methodCallExpression = ((expression != null) ? expression.Body : null) as MethodCallExpression;
			MethodInfo methodInfo;
			bool flag;
			if (methodCallExpression != null)
			{
				methodInfo = methodCallExpression.Method;
				flag = methodInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			MethodInfo result;
			if (flag2)
			{
				result = methodInfo;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CA RID: 970 RVA: 0x0000EEF8 File Offset: 0x0000D0F8
		public static PropertyInfo GetPropertyInfo<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			bool flag = expression != null;
			PropertyInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertyInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CB RID: 971 RVA: 0x0000EF20 File Offset: 0x0000D120
		public static PropertyInfo GetPropertyInfo<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			bool flag = expression != null;
			PropertyInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertyInfo(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CC RID: 972 RVA: 0x0000EF48 File Offset: 0x0000D148
		[NullableContext(1)]
		[return: Nullable(2)]
		public static PropertyInfo GetPropertyInfo(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			PropertyInfo propertyInfo;
			bool flag;
			if (memberExpression != null)
			{
				propertyInfo = memberExpression.Member as PropertyInfo;
				flag = propertyInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			PropertyInfo result;
			if (flag2)
			{
				result = propertyInfo;
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CD RID: 973 RVA: 0x0000EF8C File Offset: 0x0000D18C
		public static MethodInfo GetPropertyGetter<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertyGetter(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CE RID: 974 RVA: 0x0000EFB4 File Offset: 0x0000D1B4
		public static MethodInfo GetPropertyGetter<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertyGetter(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003CF RID: 975 RVA: 0x0000EFDC File Offset: 0x0000D1DC
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetPropertyGetter(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			PropertyInfo propertyInfo;
			bool flag;
			if (memberExpression != null)
			{
				propertyInfo = memberExpression.Member as PropertyInfo;
				flag = propertyInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			MethodInfo result;
			if (flag2)
			{
				result = ((propertyInfo != null) ? propertyInfo.GetGetMethod(true) : null);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D0 RID: 976 RVA: 0x0000F02C File Offset: 0x0000D22C
		public static MethodInfo GetPropertySetter<T>([Nullable(1)] Expression<Func<T>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertySetter(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D1 RID: 977 RVA: 0x0000F054 File Offset: 0x0000D254
		public static MethodInfo GetPropertySetter<T, TResult>([Nullable(1)] Expression<Func<T, TResult>> expression)
		{
			bool flag = expression != null;
			MethodInfo result;
			if (flag)
			{
				result = SymbolExtensions2.GetPropertySetter(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D2 RID: 978 RVA: 0x0000F07C File Offset: 0x0000D27C
		[NullableContext(1)]
		[return: Nullable(2)]
		public static MethodInfo GetPropertySetter(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			PropertyInfo propertyInfo;
			bool flag;
			if (memberExpression != null)
			{
				propertyInfo = memberExpression.Member as PropertyInfo;
				flag = propertyInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			MethodInfo result;
			if (flag2)
			{
				result = ((propertyInfo != null) ? propertyInfo.GetSetMethod(true) : null);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D3 RID: 979 RVA: 0x0000F0CC File Offset: 0x0000D2CC
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> GetStaticFieldRefAccess<[Nullable(2)] TField>(Expression<Func<TField>> expression)
		{
			bool flag = expression != null;
			AccessTools.FieldRef<TField> result;
			if (flag)
			{
				result = SymbolExtensions2.GetStaticFieldRefAccess<TField>(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D4 RID: 980 RVA: 0x0000F0F4 File Offset: 0x0000D2F4
		[NullableContext(1)]
		[return: Nullable(new byte[] { 2, 1 })]
		public static AccessTools.FieldRef<TField> GetStaticFieldRefAccess<[Nullable(2)] TField>(LambdaExpression expression)
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			FieldInfo fieldInfo;
			bool flag;
			if (memberExpression != null)
			{
				fieldInfo = memberExpression.Member as FieldInfo;
				flag = fieldInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			AccessTools.FieldRef<TField> result;
			if (flag2)
			{
				result = ((fieldInfo == null) ? null : AccessTools2.StaticFieldRefAccess<TField>(fieldInfo, true));
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D5 RID: 981 RVA: 0x0000F14C File Offset: 0x0000D34C
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<TObject, TField> GetStructFieldRefAccess<TObject, [Nullable(2)] TField>([Nullable(1)] Expression<Func<TField>> expression) where TObject : struct
		{
			bool flag = expression != null;
			AccessTools.StructFieldRef<TObject, TField> result;
			if (flag)
			{
				result = SymbolExtensions2.GetStructFieldRefAccess<TObject, TField>(expression);
			}
			else
			{
				result = null;
			}
			return result;
		}

		// Token: 0x060003D6 RID: 982 RVA: 0x0000F174 File Offset: 0x0000D374
		[NullableContext(0)]
		[return: Nullable(new byte[] { 2, 0, 1 })]
		public static AccessTools.StructFieldRef<TObject, TField> GetStructFieldRefAccess<TObject, [Nullable(2)] TField>([Nullable(1)] LambdaExpression expression) where TObject : struct
		{
			MemberExpression memberExpression = ((expression != null) ? expression.Body : null) as MemberExpression;
			FieldInfo fieldInfo;
			bool flag;
			if (memberExpression != null)
			{
				fieldInfo = memberExpression.Member as FieldInfo;
				flag = fieldInfo != null;
			}
			else
			{
				flag = false;
			}
			bool flag2 = flag;
			AccessTools.StructFieldRef<TObject, TField> result;
			if (flag2)
			{
				result = ((fieldInfo == null) ? null : AccessTools2.StructFieldRefAccess<TObject, TField>(fieldInfo, true));
			}
			else
			{
				result = null;
			}
			return result;
		}
	}
}
