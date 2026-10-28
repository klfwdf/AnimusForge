using System;
using BUTR.DependencyInjection;

namespace MCM.LightInject;

internal class LightInjectGenericServiceProviderScope : IGenericServiceProviderScope, IDisposable
{
	private readonly Scope _scope;

	public LightInjectGenericServiceProviderScope(Scope scope)
	{
		_scope = scope;
	}

	public TService? GetService<TService>() where TService : class
	{
		return _scope.GetInstance<TService>();
	}

	public void Dispose()
	{
		_scope.Dispose();
	}
}
