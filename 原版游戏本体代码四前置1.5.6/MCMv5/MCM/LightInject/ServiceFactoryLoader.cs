using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ServiceFactoryLoader
{
	public static readonly MethodInfo LoadServiceFactoryMethod;

	static ServiceFactoryLoader()
	{
		LoadServiceFactoryMethod = typeof(ServiceFactoryLoader).GetTypeInfo().GetDeclaredMethod("LoadServiceFactory");
	}

	public static IServiceFactory LoadServiceFactory(IServiceFactory serviceFactory, IScopeManager scopeManager, Scope scope)
	{
		if (scope != null)
		{
			return scope;
		}
		Scope currentScope = scopeManager.CurrentScope;
		if (currentScope != null)
		{
			return currentScope;
		}
		return serviceFactory;
	}
}
