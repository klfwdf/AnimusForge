using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class LazyHelper
{
	public static readonly MethodInfo CreateScopedLazyMethod;

	public static readonly MethodInfo CreateScopedLazyFromDelegateMethod;

	static LazyHelper()
	{
		CreateScopedLazyMethod = typeof(LazyHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedLazy");
		CreateScopedLazyFromDelegateMethod = typeof(LazyHelper).GetTypeInfo().GetDeclaredMethod("CreateScopedLazyFromDelegate");
	}

	public static Lazy<T> CreateScopedLazy<T>(ServiceContainer serviceContainer, Scope scope)
	{
		return new Lazy<T>(() => (T)serviceContainer.GetInstance(typeof(T), scope));
	}

	public static Lazy<T> CreateScopedLazyFromDelegate<T>(GetInstanceDelegate getInstanceDelegate, object[] constants, Scope scope)
	{
		return new Lazy<T>(() => (T)getInstanceDelegate(constants, scope));
	}
}
