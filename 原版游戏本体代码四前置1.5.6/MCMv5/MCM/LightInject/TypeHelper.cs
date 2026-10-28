using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class TypeHelper
{
	public static Type TryMakeGenericType(Type openGenericType, Type[] closedGenericArguments)
	{
		try
		{
			return openGenericType.MakeGenericType(closedGenericArguments);
		}
		catch (Exception)
		{
			return null;
		}
	}

	public static bool IsEnumerableOfT(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IEnumerable<>);
	}

	public static bool IsListOfT(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IList<>);
	}

	public static bool IsCollectionOfT(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(ICollection<>);
	}

	public static bool IsReadOnlyCollectionOfT(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IReadOnlyCollection<>);
	}

	public static bool IsReadOnlyListOfT(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);
	}

	public static bool IsLazy(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Lazy<>);
	}

	public static bool IsFuncRepresentingService(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(Func<>);
	}

	public static bool IsFuncWithParameters(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		if (!typeInfo.IsGenericType)
		{
			return false;
		}
		if (type.IsFuncRepresentingNamedService())
		{
			return false;
		}
		Type genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
		return genericTypeDefinition == typeof(Func<, >) || genericTypeDefinition == typeof(Func<, , >) || genericTypeDefinition == typeof(Func<, , , >) || genericTypeDefinition == typeof(Func<, , , , >);
	}

	public static bool IsFuncRepresentingNamedService(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		if (!typeInfo.IsGenericType)
		{
			return false;
		}
		Type genericTypeDefinition = typeInfo.GetGenericTypeDefinition();
		return genericTypeDefinition == typeof(Func<, >) && typeInfo.GenericTypeArguments[0] == typeof(string);
	}

	public static bool IsClosedGeneric(this Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		return typeInfo.IsGenericType && !typeInfo.IsGenericTypeDefinition;
	}

	public static Type GetElementType(Type type)
	{
		TypeInfo typeInfo = type.GetTypeInfo();
		Type[] genericTypeArguments = typeInfo.GenericTypeArguments;
		if (typeInfo.IsGenericType && genericTypeArguments.Length == 1)
		{
			return genericTypeArguments[0];
		}
		return type.GetElementType();
	}

	public static object GetDefaultValue(Type type)
	{
		MethodInfo declaredMethod = typeof(TypeHelper).GetTypeInfo().GetDeclaredMethod("GetDefaultValueInternal");
		MethodInfo methodInfo = declaredMethod.MakeGenericMethod(type);
		return methodInfo.Invoke(null, new object[0]);
	}

	private static T GetDefaultValueInternal<T>()
	{
		return default(T);
	}
}
