using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ScopeLoader
{
	public static readonly MethodInfo GetThisOrCurrentScopeMethod;

	public static readonly MethodInfo GetScopedInstanceMethod;

	public static readonly MethodInfo ValidateScopeMethod;

	public static readonly MethodInfo ValidateTrackedTransientMethod;

	static ScopeLoader()
	{
		GetThisOrCurrentScopeMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("GetThisOrCurrentScope");
		GetScopedInstanceMethod = typeof(Scope).GetTypeInfo().GetDeclaredMethod("GetScopedInstance");
		ValidateScopeMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("ValidateScope");
		ValidateTrackedTransientMethod = typeof(ScopeLoader).GetTypeInfo().GetDeclaredMethod("ValidateTrackedTransient");
	}

	public static object ValidateTrackedTransient(object instance, Scope scope)
	{
		if (instance is IDisposable)
		{
			if (scope == null)
			{
				string message = $"The disposable instance ({instance.GetType()}) was created outside a scope. If 'ContainerOptions.EnableCurrentScope=false',\nthe service must be requested directly from the scope. If `ContainerOptions.EnableCurrentScope=true`, the service can be requested from the container,\nbut either way the scope has to be started with 'container.BeginScope()'";
				throw new InvalidOperationException(message);
			}
			scope.TrackInstance(instance);
		}
		return instance;
	}

	public static Scope ValidateScope<TService>(Scope scope)
	{
		if (scope == null)
		{
			string message = $"Attempt to create a scoped instance ({typeof(TService)}) outside a scope. If 'ContainerOptions.EnableCurrentScope=false',\nthe service must be requested directly from the scope. If `ContainerOptions.EnableCurrentScope=true`, the service can be requested from the container,\nbut either way the scope has to be started with 'container.BeginScope()'";
			throw new InvalidOperationException(message);
		}
		return scope;
	}

	public static Scope GetThisOrCurrentScope(Scope scope, IScopeManager scopeManager)
	{
		if (scope != null)
		{
			return scope;
		}
		return scopeManager.CurrentScope;
	}
}
