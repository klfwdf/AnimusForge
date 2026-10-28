using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class LifetimeHelper
{
	public static readonly MethodInfo GetInstanceMethod;

	public static readonly MethodInfo GetCurrentScopeMethod;

	private static readonly ThreadSafeDictionary<Type, MethodInfo> NonClosingGetInstanceMethods;

	static LifetimeHelper()
	{
		NonClosingGetInstanceMethods = new ThreadSafeDictionary<Type, MethodInfo>();
		GetInstanceMethod = typeof(ILifetime).GetTypeInfo().GetDeclaredMethod("GetInstance");
		GetCurrentScopeMethod = typeof(IScopeManager).GetTypeInfo().GetDeclaredProperty("CurrentScope").GetMethod;
	}

	public static MethodInfo GetNonClosingGetInstanceMethod(Type lifetimeType)
	{
		return NonClosingGetInstanceMethods.GetOrAdd(lifetimeType, ResolveNonClosingGetInstanceMethod);
	}

	private static MethodInfo ResolveNonClosingGetInstanceMethod(Type lifetimeType)
	{
		Type[] parameterTypes = new Type[3]
		{
			typeof(GetInstanceDelegate),
			typeof(Scope),
			typeof(object[])
		};
		return lifetimeType.GetTypeInfo().DeclaredMethods.SingleOrDefault((MethodInfo m) => (from p in m.GetParameters()
			select p.ParameterType).SequenceEqual(parameterTypes));
	}
}
