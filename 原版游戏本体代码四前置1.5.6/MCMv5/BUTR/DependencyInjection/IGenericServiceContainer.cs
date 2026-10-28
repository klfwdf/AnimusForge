using System;

namespace BUTR.DependencyInjection;

public interface IGenericServiceContainer
{
	IGenericServiceContainer RegisterSingleton<TService>() where TService : class;

	IGenericServiceContainer RegisterSingleton<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class;

	IGenericServiceContainer RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterSingleton<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterSingleton(Type serviceType, Type implementationType);

	IGenericServiceContainer RegisterSingleton(Type serviceType, Func<object> factory);

	IGenericServiceContainer RegisterScoped<TService>() where TService : class;

	IGenericServiceContainer RegisterScoped<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class;

	IGenericServiceContainer RegisterScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterScoped<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterScoped(Type serviceType, Type implementationType);

	IGenericServiceContainer RegisterScoped(Type serviceType, Func<object> factory);

	IGenericServiceContainer RegisterTransient<TService>() where TService : class;

	IGenericServiceContainer RegisterTransient<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class;

	IGenericServiceContainer RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterTransient<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService;

	IGenericServiceContainer RegisterTransient(Type serviceType, Type implementationType);

	IGenericServiceContainer RegisterTransient(Type serviceType, Func<object> factory);

	IGenericServiceProvider Build();
}
