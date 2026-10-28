using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ServiceFactoryExtensions
{
	public static TService GetInstance<TService>(this IServiceFactory factory)
	{
		return (TService)factory.GetInstance(typeof(TService));
	}

	public static TService GetInstance<TService>(this IServiceFactory factory, string serviceName)
	{
		return (TService)factory.GetInstance(typeof(TService), serviceName);
	}

	public static TService GetInstance<T, TService>(this IServiceFactory factory, T value)
	{
		return (TService)factory.GetInstance(typeof(TService), new object[1] { value });
	}

	public static TService GetInstance<T, TService>(this IServiceFactory factory, T value, string serviceName)
	{
		return (TService)factory.GetInstance(typeof(TService), serviceName, new object[1] { value });
	}

	public static TService GetInstance<T1, T2, TService>(this IServiceFactory factory, T1 arg1, T2 arg2)
	{
		return (TService)factory.GetInstance(typeof(TService), new object[2] { arg1, arg2 });
	}

	public static TService GetInstance<T1, T2, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, string serviceName)
	{
		return (TService)factory.GetInstance(typeof(TService), serviceName, new object[2] { arg1, arg2 });
	}

	public static TService GetInstance<T1, T2, T3, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3)
	{
		return (TService)factory.GetInstance(typeof(TService), new object[3] { arg1, arg2, arg3 });
	}

	public static TService GetInstance<T1, T2, T3, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, string serviceName)
	{
		return (TService)factory.GetInstance(typeof(TService), serviceName, new object[3] { arg1, arg2, arg3 });
	}

	public static TService GetInstance<T1, T2, T3, T4, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
	{
		return (TService)factory.GetInstance(typeof(TService), new object[4] { arg1, arg2, arg3, arg4 });
	}

	public static TService GetInstance<T1, T2, T3, T4, TService>(this IServiceFactory factory, T1 arg1, T2 arg2, T3 arg3, T4 arg4, string serviceName)
	{
		return (TService)factory.GetInstance(typeof(TService), serviceName, new object[4] { arg1, arg2, arg3, arg4 });
	}

	public static TService TryGetInstance<TService>(this IServiceFactory factory)
	{
		return (TService)factory.TryGetInstance(typeof(TService));
	}

	public static TService TryGetInstance<TService>(this IServiceFactory factory, string serviceName)
	{
		return (TService)factory.TryGetInstance(typeof(TService), serviceName);
	}

	public static IEnumerable<TService> GetAllInstances<TService>(this IServiceFactory factory)
	{
		return factory.GetInstance<IEnumerable<TService>>();
	}

	public static TService Create<TService>(this IServiceFactory factory) where TService : class
	{
		return (TService)factory.Create(typeof(TService));
	}
}
