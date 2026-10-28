using System;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib;

internal class ButterLibGenericServiceProviderScope : IGenericServiceProviderScope, IDisposable
{
	private readonly IServiceScope _serviceScope;

	public ButterLibGenericServiceProviderScope(IServiceScope serviceScope)
	{
		_serviceScope = serviceScope;
	}

	public TService? GetService<TService>() where TService : class
	{
		return _serviceScope.ServiceProvider.GetRequiredService<TService>();
	}

	public void Dispose()
	{
		_serviceScope.Dispose();
	}
}
