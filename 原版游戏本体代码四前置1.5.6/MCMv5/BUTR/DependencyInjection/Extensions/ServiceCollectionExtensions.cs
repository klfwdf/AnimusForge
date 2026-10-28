using System;

namespace BUTR.DependencyInjection.Extensions;

public static class ServiceCollectionExtensions
{
	internal static WithHistoryGenericServiceContainer ServiceContainer;

	public static IGenericServiceContainer AddSingleton<TService>(this IGenericServiceContainer services) where TService : class
	{
		services.RegisterSingleton<TService>();
		return services;
	}

	public static IGenericServiceContainer AddSingleton<TService>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterSingleton(factory);
		return services;
	}

	public static IGenericServiceContainer AddSingleton<TService, TImplementation>(this IGenericServiceContainer services) where TService : class where TImplementation : class, TService
	{
		services.RegisterSingleton<TService, TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddSingleton<TService, TImplementation>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterSingleton(factory);
		return services;
	}

	public static IGenericServiceContainer AddSingleton(this IGenericServiceContainer services, Type serviceType, Type implementationType)
	{
		services.RegisterSingleton(serviceType, implementationType);
		return services;
	}

	public static IGenericServiceContainer AddSingleton(this IGenericServiceContainer services, Type serviceType, Func<object> factory)
	{
		services.RegisterSingleton(serviceType, factory);
		return services;
	}

	public static IGenericServiceContainer AddScoped<TService>(this IGenericServiceContainer services) where TService : class
	{
		services.RegisterScoped<TService>();
		return services;
	}

	public static IGenericServiceContainer AddScoped<TService, TImplementation>(this IGenericServiceContainer services) where TService : class where TImplementation : class, TService
	{
		services.RegisterScoped<TService, TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddScoped<TService>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterScoped(factory);
		return services;
	}

	public static IGenericServiceContainer AddScoped<TService, TImplementation>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterScoped(factory);
		return services;
	}

	public static IGenericServiceContainer AddScoped(this IGenericServiceContainer services, Type serviceType, Type implementationType)
	{
		services.RegisterScoped(serviceType, implementationType);
		return services;
	}

	public static IGenericServiceContainer AddScoped(this IGenericServiceContainer services, Type serviceType, Func<object> factory)
	{
		services.RegisterScoped(serviceType, factory);
		return services;
	}

	public static IGenericServiceContainer AddTransient<TService>(this IGenericServiceContainer services) where TService : class
	{
		services.RegisterTransient<TService>();
		return services;
	}

	public static IGenericServiceContainer AddTransient<TService, TImplementation>(this IGenericServiceContainer services) where TService : class where TImplementation : class, TService
	{
		services.RegisterTransient<TService, TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddTransient<TService>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterTransient(factory);
		return services;
	}

	public static IGenericServiceContainer AddTransient<TService, TImplementation>(this IGenericServiceContainer services, Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		services.RegisterTransient(factory);
		return services;
	}

	public static IGenericServiceContainer AddTransient(this IGenericServiceContainer services, Type serviceType, Type implementationType)
	{
		services.RegisterTransient(serviceType, implementationType);
		return services;
	}

	public static IGenericServiceContainer AddTransient(this IGenericServiceContainer services, Type serviceType, Func<object> factory)
	{
		services.RegisterTransient(serviceType, factory);
		return services;
	}
}
