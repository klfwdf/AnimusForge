using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace HarmonyLib.BUTR.Extensions
{
	// Token: 0x02000014 RID: 20
	[NullableContext(2)]
	[Nullable(0)]
	internal static class SymbolExtensions2
	{
		// Token: 0x06000095 RID: 149 RVA: 0x000052C0 File Offset: 0x000034C0
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

		// Token: 0x06000096 RID: 150 RVA: 0x000052E8 File Offset: 0x000034E8
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

		// Token: 0x06000097 RID: 151 RVA: 0x00005310 File Offset: 0x00003510
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

		// Token: 0x06000098 RID: 152 RVA: 0x00005338 File Offset: 0x00003538
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

		// Token: 0x06000099 RID: 153 RVA: 0x00005360 File Offset: 0x00003560
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

		// Token: 0x0600009A RID: 154 RVA: 0x00005388 File Offset: 0x00003588
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

		// Token: 0x0600009B RID: 155 RVA: 0x000053B0 File Offset: 0x000035B0
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

		// Token: 0x0600009C RID: 156 RVA: 0x000053D8 File Offset: 0x000035D8
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

		// Token: 0x0600009D RID: 157 RVA: 0x00005400 File Offset: 0x00003600
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

		// Token: 0x0600009E RID: 158 RVA: 0x00005428 File Offset: 0x00003628
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

		// Token: 0x0600009F RID: 159 RVA: 0x00005450 File Offset: 0x00003650
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

		// Token: 0x060000A0 RID: 160 RVA: 0x00005494 File Offset: 0x00003694
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

		// Token: 0x060000A1 RID: 161 RVA: 0x000054BC File Offset: 0x000036BC
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

		// Token: 0x060000A2 RID: 162 RVA: 0x000054E4 File Offset: 0x000036E4
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

		// Token: 0x060000A3 RID: 163 RVA: 0x00005528 File Offset: 0x00003728
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

		// Token: 0x060000A4 RID: 164 RVA: 0x00005550 File Offset: 0x00003750
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

		// Token: 0x060000A5 RID: 165 RVA: 0x000055A8 File Offset: 0x000037A8
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

		// Token: 0x060000A6 RID: 166 RVA: 0x000055D0 File Offset: 0x000037D0
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

		// Token: 0x060000A7 RID: 167 RVA: 0x00005628 File Offset: 0x00003828
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

		// Token: 0x060000A8 RID: 168 RVA: 0x00005650 File Offset: 0x00003850
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

		// Token: 0x060000A9 RID: 169 RVA: 0x00005678 File Offset: 0x00003878
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

		// Token: 0x060000AA RID: 170 RVA: 0x000056A0 File Offset: 0x000038A0
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

		// Token: 0x060000AB RID: 171 RVA: 0x000056C8 File Offset: 0x000038C8
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

		// Token: 0x060000AC RID: 172 RVA: 0x000056F0 File Offset: 0x000038F0
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

		// Token: 0x060000AD RID: 173 RVA: 0x00005718 File Offset: 0x00003918
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

		// Token: 0x060000AE RID: 174 RVA: 0x00005740 File Offset: 0x00003940
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

		// Token: 0x060000AF RID: 175 RVA: 0x00005768 File Offset: 0x00003968
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

		// Token: 0x060000B0 RID: 176 RVA: 0x00005790 File Offset: 0x00003990
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

		// Token: 0x060000B1 RID: 177 RVA: 0x000057B8 File Offset: 0x000039B8
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

		// Token: 0x060000B2 RID: 178 RVA: 0x000057E0 File Offset: 0x000039E0
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

		// Token: 0x060000B3 RID: 179 RVA: 0x00005808 File Offset: 0x00003A08
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

		// Token: 0x060000B4 RID: 180 RVA: 0x00005830 File Offset: 0x00003A30
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

		// Token: 0x060000B5 RID: 181 RVA: 0x00005858 File Offset: 0x00003A58
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

		// Token: 0x060000B6 RID: 182 RVA: 0x00005880 File Offset: 0x00003A80
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

		// Token: 0x060000B7 RID: 183 RVA: 0x000058A8 File Offset: 0x00003AA8
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

		// Token: 0x060000B8 RID: 184 RVA: 0x000058D0 File Offset: 0x00003AD0
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

		// Token: 0x060000B9 RID: 185 RVA: 0x000058F8 File Offset: 0x00003AF8
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

		// Token: 0x060000BA RID: 186 RVA: 0x00005920 File Offset: 0x00003B20
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

		// Token: 0x060000BB RID: 187 RVA: 0x00005948 File Offset: 0x00003B48
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

		// Token: 0x060000BC RID: 188 RVA: 0x00005988 File Offset: 0x00003B88
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

		// Token: 0x060000BD RID: 189 RVA: 0x000059B0 File Offset: 0x00003BB0
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

		// Token: 0x060000BE RID: 190 RVA: 0x000059D8 File Offset: 0x00003BD8
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

		// Token: 0x060000BF RID: 191 RVA: 0x00005A1C File Offset: 0x00003C1C
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

		// Token: 0x060000C0 RID: 192 RVA: 0x00005A44 File Offset: 0x00003C44
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

		// Token: 0x060000C1 RID: 193 RVA: 0x00005A6C File Offset: 0x00003C6C
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

		// Token: 0x060000C2 RID: 194 RVA: 0x00005ABC File Offset: 0x00003CBC
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

		// Token: 0x060000C3 RID: 195 RVA: 0x00005AE4 File Offset: 0x00003CE4
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

		// Token: 0x060000C4 RID: 196 RVA: 0x00005B0C File Offset: 0x00003D0C
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

		// Token: 0x060000C5 RID: 197 RVA: 0x00005B5C File Offset: 0x00003D5C
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

		// Token: 0x060000C6 RID: 198 RVA: 0x00005B84 File Offset: 0x00003D84
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

		// Token: 0x060000C7 RID: 199 RVA: 0x00005BDC File Offset: 0x00003DDC
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

		// Token: 0x060000C8 RID: 200 RVA: 0x00005C04 File Offset: 0x00003E04
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
