using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HarmonyLib.BUTR.Extensions;

internal static class SymbolExtensions2
{
	public static ConstructorInfo? GetConstructorInfo<TResult>(Expression<Func<TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, TResult>(Expression<Func<T1, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
	{
		if (expression != null)
		{
			return GetConstructorInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static ConstructorInfo? GetConstructorInfo(LambdaExpression expression)
	{
		if (expression?.Body is NewExpression { Constructor: not null } newExpression)
		{
			return newExpression.Constructor;
		}
		return null;
	}

	public static FieldInfo? GetFieldInfo<T>(Expression<Func<T>> expression)
	{
		if (expression != null)
		{
			return GetFieldInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static FieldInfo? GetFieldInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression != null)
		{
			return GetFieldInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static FieldInfo? GetFieldInfo(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: FieldInfo member })
		{
			return member;
		}
		return null;
	}

	public static AccessTools.FieldRef<object, TField>? GetFieldRefAccess<TField>(Expression<Func<TField>> expression)
	{
		if (expression != null)
		{
			return SymbolExtensions2.GetFieldRefAccess<TField>((LambdaExpression)expression);
		}
		return null;
	}

	public static AccessTools.FieldRef<object, TField>? GetFieldRefAccess<TField>(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: FieldInfo member })
		{
			return (member == null) ? null : AccessTools2.FieldRefAccess<object, TField>(member);
		}
		return null;
	}

	public static AccessTools.FieldRef<TObject, TField>? GetFieldRefAccess<TObject, TField>(Expression<Func<TObject, TField>> expression) where TObject : class
	{
		if (expression != null)
		{
			return SymbolExtensions2.GetFieldRefAccess<TObject, TField>((LambdaExpression)expression);
		}
		return null;
	}

	public static AccessTools.FieldRef<TObject, TField>? GetFieldRefAccess<TObject, TField>(LambdaExpression expression) where TObject : class
	{
		if (expression?.Body is MemberExpression { Member: FieldInfo member })
		{
			return (member == null) ? null : AccessTools2.FieldRefAccess<TObject, TField>(member);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo(Expression<Action> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1>(Expression<Action<T1>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2>(Expression<Action<T1, T2>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3>(Expression<Action<T1, T2, T3>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4>(Expression<Action<T1, T2, T3, T4>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5>(Expression<Action<T1, T2, T3, T4, T5>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6>(Expression<Action<T1, T2, T3, T4, T5, T6>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7>(Expression<Action<T1, T2, T3, T4, T5, T6, T7>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8>(Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9>(Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<TResult>(Expression<Func<TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, TResult>(Expression<Func<T1, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, TResult>(Expression<Func<T1, T2, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, TResult>(Expression<Func<T1, T2, T3, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, TResult>(Expression<Func<T1, T2, T3, T4, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, TResult>(Expression<Func<T1, T2, T3, T4, T5, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, TResult>(Expression<Func<T1, T2, T3, T4, T5, T6, T7, T8, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>(Expression<Action<T1, T2, T3, T4, T5, T6, T7, T8, T9, TResult>> expression)
	{
		if (expression != null)
		{
			return GetMethodInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetMethodInfo(LambdaExpression expression)
	{
		if (expression?.Body is MethodCallExpression methodCallExpression)
		{
			MethodInfo method = methodCallExpression.Method;
			if ((object)method != null)
			{
				return method;
			}
		}
		return null;
	}

	public static PropertyInfo? GetPropertyInfo<T>(Expression<Func<T>> expression)
	{
		if (expression != null)
		{
			return GetPropertyInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static PropertyInfo? GetPropertyInfo<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression != null)
		{
			return GetPropertyInfo((LambdaExpression)expression);
		}
		return null;
	}

	public static PropertyInfo? GetPropertyInfo(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: PropertyInfo member })
		{
			return member;
		}
		return null;
	}

	public static MethodInfo? GetPropertyGetter<T>(Expression<Func<T>> expression)
	{
		if (expression != null)
		{
			return GetPropertyGetter((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetPropertyGetter<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression != null)
		{
			return GetPropertyGetter((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetPropertyGetter(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: PropertyInfo member })
		{
			return member?.GetGetMethod(nonPublic: true);
		}
		return null;
	}

	public static MethodInfo? GetPropertySetter<T>(Expression<Func<T>> expression)
	{
		if (expression != null)
		{
			return GetPropertySetter((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetPropertySetter<T, TResult>(Expression<Func<T, TResult>> expression)
	{
		if (expression != null)
		{
			return GetPropertySetter((LambdaExpression)expression);
		}
		return null;
	}

	public static MethodInfo? GetPropertySetter(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: PropertyInfo member })
		{
			return member?.GetSetMethod(nonPublic: true);
		}
		return null;
	}

	public static AccessTools.FieldRef<TField>? GetStaticFieldRefAccess<TField>(Expression<Func<TField>> expression)
	{
		if (expression != null)
		{
			return GetStaticFieldRefAccess<TField>((LambdaExpression)expression);
		}
		return null;
	}

	public static AccessTools.FieldRef<TField>? GetStaticFieldRefAccess<TField>(LambdaExpression expression)
	{
		if (expression?.Body is MemberExpression { Member: FieldInfo member })
		{
			return (member == null) ? null : AccessTools2.StaticFieldRefAccess<TField>(member);
		}
		return null;
	}

	public static AccessTools.StructFieldRef<TObject, TField>? GetStructFieldRefAccess<TObject, TField>(Expression<Func<TField>> expression) where TObject : struct
	{
		if (expression != null)
		{
			return GetStructFieldRefAccess<TObject, TField>((LambdaExpression)expression);
		}
		return null;
	}

	public static AccessTools.StructFieldRef<TObject, TField>? GetStructFieldRefAccess<TObject, TField>(LambdaExpression expression) where TObject : struct
	{
		if (expression?.Body is MemberExpression { Member: FieldInfo member })
		{
			return (member == null) ? null : AccessTools2.StructFieldRefAccess<TObject, TField>(member);
		}
		return null;
	}
}
