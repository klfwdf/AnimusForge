using BUTR.DependencyInjection;
using BUTR.DependencyInjection.Extensions;
using TaleWorlds.MountAndBlade;

namespace MCM.Internal.Extensions;

public static class ServiceCollectionExtensions
{
	public static IGenericServiceContainer GetServiceContainer(this MBSubModuleBase _)
	{
		return BUTR.DependencyInjection.Extensions.ServiceCollectionExtensions.ServiceContainer;
	}
}
