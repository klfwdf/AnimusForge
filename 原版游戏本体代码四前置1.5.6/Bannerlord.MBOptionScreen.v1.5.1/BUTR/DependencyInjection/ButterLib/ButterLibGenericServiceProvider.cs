using System;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;
using TaleWorlds.MountAndBlade;

namespace BUTR.DependencyInjection.ButterLib;

internal class ButterLibGenericServiceProvider : IGenericServiceProvider, IDisposable
{
	private static IServiceProvider? ServiceProvider => ((MBSubModuleBase)null).GetTempServiceProvider() ?? ((MBSubModuleBase)null).GetServiceProvider();

	public IGenericServiceProviderScope CreateScope()
	{
		return new ButterLibGenericServiceProviderScope(ServiceProvider.CreateScope());
	}

	public TService? GetService<TService>() where TService : class
	{
		IServiceProvider? serviceProvider = ServiceProvider;
		if (serviceProvider == null)
		{
			return null;
		}
		return serviceProvider.GetRequiredService<TService>();
	}

	public void Dispose()
	{
	}
}
