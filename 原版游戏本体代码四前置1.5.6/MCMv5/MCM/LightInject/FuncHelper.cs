using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class FuncHelper
{
	public static readonly MethodInfo CreateScopedFuncMethod;

	public static readonly MethodInfo CreateScopedGenericFuncMethod;

	public static readonly MethodInfo CreateScopedGenericNamedFuncMethod;

	static FuncHelper()
	{
		CreateScopedFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedFunc");
		CreateScopedGenericFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedGenericFunc");
		CreateScopedGenericNamedFuncMethod = typeof(FuncHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedGenericNamedFunc");
	}

	public static Func<object> CreateScopedFunc(GetInstanceDelegate getInstanceDelegate, object[] constants, Scope scope)
	{
		return () => getInstanceDelegate(constants, scope);
	}

	public static Func<T> CreateScopedGenericFunc<T>(ServiceContainer serviceContainer, Scope scope)
	{
		return () => (T)serviceContainer.GetInstance(typeof(T), scope);
	}

	public static Func<string, T> CreateScopedGenericNamedFunc<T>(ServiceContainer serviceContainer, Scope scope)
	{
		return (string serviceName) => (T)serviceContainer.GetInstance(typeof(T), scope, serviceName);
	}
}
