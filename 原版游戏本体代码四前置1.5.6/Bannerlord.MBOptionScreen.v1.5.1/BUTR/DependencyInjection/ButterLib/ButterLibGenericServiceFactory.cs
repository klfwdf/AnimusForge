using System;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib;

internal class ButterLibGenericServiceFactory : IGenericServiceFactory
{
	private readonly IServiceProvider _serviceProvider;

	public ButterLibGenericServiceFactory(IServiceProvider factory)
	{
		_serviceProvider = factory;
	}

	public TService GetService<TService>() where TService : class
	{
		return _serviceProvider.GetRequiredService<TService>();
	}
}
