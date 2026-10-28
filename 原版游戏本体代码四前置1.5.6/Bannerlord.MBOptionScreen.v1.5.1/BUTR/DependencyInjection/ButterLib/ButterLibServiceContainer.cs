using System;
using Bannerlord.ButterLib.Common.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace BUTR.DependencyInjection.ButterLib;

internal class ButterLibServiceContainer : IGenericServiceContainer
{
	private static IServiceCollection? ServiceContainer => ((MBSubModuleBase)null).GetServices();

	public IGenericServiceContainer RegisterSingleton<TService>() where TService : class
	{
		ServiceContainer.AddSingleton<TService>();
		return this;
	}

	public IGenericServiceContainer RegisterSingleton<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		ServiceContainer.AddSingleton((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
		return this;
	}

	public IGenericServiceContainer RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddSingleton<TService, TImplementation>();
		return this;
	}

	public IGenericServiceContainer RegisterSingleton<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddSingleton<TService, TImplementation>((Func<IServiceProvider, TImplementation>)((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp))));
		return this;
	}

	public IGenericServiceContainer RegisterSingleton(Type serviceType, Type implementationType)
	{
		ServiceContainer.AddSingleton(serviceType, implementationType);
		return this;
	}

	public IGenericServiceContainer RegisterSingleton(Type serviceType, Func<object> factory)
	{
		ServiceContainer.AddSingleton((IServiceProvider _) => factory());
		return this;
	}

	public IGenericServiceContainer RegisterScoped<TService>() where TService : class
	{
		ServiceContainer.AddScoped<TService>();
		return this;
	}

	public IGenericServiceContainer RegisterScoped<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		ServiceContainer.AddScoped((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
		return this;
	}

	public IGenericServiceContainer RegisterScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddScoped<TService, TImplementation>();
		return this;
	}

	public IGenericServiceContainer RegisterScoped<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddScoped<TService, TImplementation>((Func<IServiceProvider, TImplementation>)((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp))));
		return this;
	}

	public IGenericServiceContainer RegisterScoped(Type serviceType, Type implementationType)
	{
		ServiceContainer.AddScoped(serviceType, implementationType);
		return this;
	}

	public IGenericServiceContainer RegisterScoped(Type serviceType, Func<object> factory)
	{
		ServiceContainer.AddScoped((IServiceProvider _) => factory());
		return this;
	}

	public IGenericServiceContainer RegisterTransient<TService>() where TService : class
	{
		ServiceContainer.AddTransient<TService>();
		return this;
	}

	public IGenericServiceContainer RegisterTransient<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		ServiceContainer.AddTransient((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp)));
		return this;
	}

	public IGenericServiceContainer RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddTransient<TService, TImplementation>();
		return this;
	}

	public IGenericServiceContainer RegisterTransient<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		ServiceContainer.AddTransient<TService, TImplementation>((Func<IServiceProvider, TImplementation>)((IServiceProvider sp) => factory(new ButterLibGenericServiceFactory(sp))));
		return this;
	}

	public IGenericServiceContainer RegisterTransient(Type serviceType, Type implementationType)
	{
		ServiceContainer.AddTransient(serviceType, implementationType);
		return this;
	}

	public IGenericServiceContainer RegisterTransient(Type serviceType, Func<object> factory)
	{
		ServiceContainer.AddTransient(serviceType, (IServiceProvider _) => factory());
		return this;
	}

	public IGenericServiceProvider Build()
	{
		return new ButterLibGenericServiceProvider();
	}
}
