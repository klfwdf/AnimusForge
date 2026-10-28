using System;
using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal static class ServiceRegistryExtensions
{
	public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory)
	{
		return serviceRegistry.Register(serviceType, factory, string.Empty, null);
	}

	public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, ILifetime lifetime)
	{
		return serviceRegistry.Register(serviceType, factory, string.Empty, lifetime);
	}

	public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName)
	{
		return serviceRegistry.Register(serviceType, factory, serviceName, null);
	}

	public static IServiceRegistry Register(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName, ILifetime lifetime)
	{
		ServiceRegistration serviceRegistration = new ServiceRegistration
		{
			FactoryExpression = factory,
			ServiceType = serviceType,
			ServiceName = serviceName,
			Lifetime = lifetime
		};
		return serviceRegistry.Register(serviceRegistration);
	}

	public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory)
	{
		return serviceRegistry.RegisterSingleton(serviceType, factory, string.Empty);
	}

	public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName)
	{
		return serviceRegistry.Register(serviceType, factory, serviceName, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory)
	{
		return serviceRegistry.RegisterScoped(serviceType, factory, string.Empty);
	}

	public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName)
	{
		return serviceRegistry.Register(serviceType, factory, serviceName, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory)
	{
		return serviceRegistry.RegisterTransient(serviceType, factory, string.Empty);
	}

	public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Func<IServiceFactory, object> factory, string serviceName)
	{
		return serviceRegistry.Register(serviceType, factory, serviceName);
	}

	public static IServiceRegistry RegisterSingleton<TService, TImplementation>(this IServiceRegistry serviceRegistry) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>(new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry)
	{
		return serviceRegistry.Register<TService>(new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>(serviceName, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
	{
		return serviceRegistry.Register(serviceType, implementingType, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType)
	{
		return serviceRegistry.Register(serviceType, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
	{
		return serviceRegistry.Register(serviceType, implementingType, serviceName, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
	{
		return serviceRegistry.Register(factory, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterSingleton<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
	{
		return serviceRegistry.Register(factory, serviceName, new PerContainerLifetime());
	}

	public static IServiceRegistry RegisterScoped<TService, TImplementation>(this IServiceRegistry serviceRegistry) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>(new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry)
	{
		return serviceRegistry.Register<TService>(new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>(serviceName, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
	{
		return serviceRegistry.Register(serviceType, implementingType, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType)
	{
		return serviceRegistry.Register(serviceType, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
	{
		return serviceRegistry.Register(serviceType, implementingType, serviceName, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
	{
		return serviceRegistry.Register(factory, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterScoped<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
	{
		return serviceRegistry.Register(factory, serviceName, new PerScopeLifetime());
	}

	public static IServiceRegistry RegisterTransient<TService, TImplementation>(this IServiceRegistry serviceRegistry) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>();
	}

	public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry)
	{
		return serviceRegistry.Register<TService>();
	}

	public static IServiceRegistry RegisterTransient<TService, TImplementation>(this IServiceRegistry serviceRegistry, string serviceName) where TImplementation : TService
	{
		return serviceRegistry.Register<TService, TImplementation>(serviceName);
	}

	public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType)
	{
		return serviceRegistry.Register(serviceType, implementingType);
	}

	public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType)
	{
		return serviceRegistry.Register(serviceType);
	}

	public static IServiceRegistry RegisterTransient(this IServiceRegistry serviceRegistry, Type serviceType, Type implementingType, string serviceName)
	{
		return serviceRegistry.Register(serviceType, implementingType, serviceName);
	}

	public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory)
	{
		return serviceRegistry.Register(factory);
	}

	public static IServiceRegistry RegisterTransient<TService>(this IServiceRegistry serviceRegistry, Func<IServiceFactory, TService> factory, string serviceName)
	{
		return serviceRegistry.Register(factory, serviceName);
	}

	public static IServiceRegistry Override<TService, TImplementation>(this IServiceRegistry serviceRegistry) where TImplementation : TService
	{
		return serviceRegistry.Override((ServiceRegistration sr) => sr.ServiceType == typeof(TService), delegate(IServiceFactory serviceFactory, ServiceRegistration registration)
		{
			registration.FactoryExpression = null;
			registration.ImplementingType = typeof(TImplementation);
			return registration;
		});
	}

	public static IServiceRegistry Override<TService, TImplementation>(this IServiceRegistry serviceRegistry, ILifetime lifetime) where TImplementation : TService
	{
		return serviceRegistry.Override((ServiceRegistration sr) => sr.ServiceType == typeof(TService), delegate(IServiceFactory serviceFactory, ServiceRegistration registration)
		{
			registration.FactoryExpression = null;
			registration.ImplementingType = typeof(TImplementation);
			registration.Lifetime = lifetime;
			return registration;
		});
	}

	public static IServiceRegistry Initialize<TService>(this IServiceRegistry serviceRegistry, Action<IServiceFactory, TService> processor)
	{
		return serviceRegistry.Initialize((ServiceRegistration sr) => sr.ServiceType == typeof(TService), delegate(IServiceFactory factory, object instance)
		{
			processor(factory, (TService)instance);
		});
	}
}
