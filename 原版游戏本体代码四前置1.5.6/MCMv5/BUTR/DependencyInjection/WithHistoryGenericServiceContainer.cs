using System;
using System.Collections.Generic;

namespace BUTR.DependencyInjection;

public class WithHistoryGenericServiceContainer : IGenericServiceContainer
{
	private readonly IGenericServiceContainer _serviceContainer;

	public List<Action<IGenericServiceContainer>> History { get; } = new List<Action<IGenericServiceContainer>>();

	public WithHistoryGenericServiceContainer(IGenericServiceContainer serviceContainer)
	{
		_serviceContainer = serviceContainer;
	}

	public IGenericServiceContainer RegisterSingleton<TService>() where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton<TService>();
		});
		return _serviceContainer.RegisterSingleton<TService>();
	}

	public IGenericServiceContainer RegisterSingleton<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton(factory);
		});
		return _serviceContainer.RegisterSingleton(factory);
	}

	public IGenericServiceContainer RegisterSingleton<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton<TService, TImplementation>();
		});
		return _serviceContainer.RegisterSingleton<TService, TImplementation>();
	}

	public IGenericServiceContainer RegisterSingleton<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton<TService, TImplementation>(factory);
		});
		return _serviceContainer.RegisterSingleton<TService, TImplementation>(factory);
	}

	public IGenericServiceContainer RegisterSingleton(Type serviceType, Type implementationType)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton(serviceType, implementationType);
		});
		return _serviceContainer.RegisterSingleton(serviceType, implementationType);
	}

	public IGenericServiceContainer RegisterSingleton(Type serviceType, Func<object> factory)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterSingleton(serviceType, factory);
		});
		return _serviceContainer.RegisterSingleton(serviceType, factory);
	}

	public IGenericServiceContainer RegisterScoped<TService>() where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped<TService>();
		});
		return _serviceContainer.RegisterScoped<TService>();
	}

	public IGenericServiceContainer RegisterScoped<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped(factory);
		});
		return _serviceContainer.RegisterScoped(factory);
	}

	public IGenericServiceContainer RegisterScoped<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped<TService, TImplementation>();
		});
		return _serviceContainer.RegisterScoped<TService, TImplementation>();
	}

	public IGenericServiceContainer RegisterScoped<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped<TService, TImplementation>(factory);
		});
		return _serviceContainer.RegisterScoped<TService, TImplementation>(factory);
	}

	public IGenericServiceContainer RegisterScoped(Type serviceType, Type implementationType)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped(serviceType, implementationType);
		});
		return _serviceContainer.RegisterScoped(serviceType, implementationType);
	}

	public IGenericServiceContainer RegisterScoped(Type serviceType, Func<object> factory)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterScoped(serviceType, factory);
		});
		return _serviceContainer.RegisterScoped(serviceType, factory);
	}

	public IGenericServiceContainer RegisterTransient<TService>() where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient<TService>();
		});
		return _serviceContainer.RegisterTransient<TService>();
	}

	public IGenericServiceContainer RegisterTransient<TService>(Func<IGenericServiceFactory, TService> factory) where TService : class
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient(factory);
		});
		return _serviceContainer.RegisterTransient(factory);
	}

	public IGenericServiceContainer RegisterTransient<TService, TImplementation>() where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient<TService, TImplementation>();
		});
		return _serviceContainer.RegisterTransient<TService, TImplementation>();
	}

	public IGenericServiceContainer RegisterTransient<TService, TImplementation>(Func<IGenericServiceFactory, TImplementation> factory) where TService : class where TImplementation : class, TService
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient<TService, TImplementation>(factory);
		});
		return _serviceContainer.RegisterTransient<TService, TImplementation>(factory);
	}

	public IGenericServiceContainer RegisterTransient(Type serviceType, Type implementationType)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient(serviceType, implementationType);
		});
		return _serviceContainer.RegisterTransient(serviceType, implementationType);
	}

	public IGenericServiceContainer RegisterTransient(Type serviceType, Func<object> factory)
	{
		History.Add(delegate(IGenericServiceContainer serviceContainer)
		{
			serviceContainer.RegisterTransient(serviceType, factory);
		});
		return _serviceContainer.RegisterTransient(serviceType, factory);
	}

	public IGenericServiceProvider Build()
	{
		History.Clear();
		return _serviceContainer.Build();
	}
}
