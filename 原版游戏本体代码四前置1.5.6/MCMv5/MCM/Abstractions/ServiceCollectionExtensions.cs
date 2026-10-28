using System;
using BUTR.DependencyInjection;
using MCM.Abstractions.FluentBuilder;
using MCM.Abstractions.Global;
using MCM.Abstractions.PerCampaign;
using MCM.Abstractions.PerSave;
using MCM.Abstractions.Properties;

namespace MCM.Abstractions;

public static class ServiceCollectionExtensions
{
	public static IGenericServiceContainer AddSettingsProvider<TService, TImplementation>(this IGenericServiceContainer services) where TService : BaseSettingsProvider where TImplementation : class, TService
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((IGenericServiceFactory sp) => (TService)(BaseSettingsProvider)sp.GetService<TImplementation>());
		services.AddSettingsProvider<TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddSettingsProvider<TImplementation>(this IGenericServiceContainer services) where TImplementation : BaseSettingsProvider
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, BaseSettingsProvider>)((IGenericServiceFactory sp) => sp.GetService<TImplementation>()));
		return services;
	}

	public static IGenericServiceContainer AddSettingsFormat<TService, TImplementation>(this IGenericServiceContainer services) where TService : class, ISettingsFormat where TImplementation : class, TService
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((IGenericServiceFactory sp) => (TService)(object)sp.GetService<TImplementation>());
		services.AddSettingsFormat<TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddSettingsFormat<TImplementation>(this IGenericServiceContainer services) where TImplementation : class, ISettingsFormat
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, ISettingsFormat>)((IGenericServiceFactory sp) => sp.GetService<TImplementation>()));
		return services;
	}

	public static IGenericServiceContainer AddSettingsPropertyDiscoverer<TService, TImplementation>(this IGenericServiceContainer services) where TService : class, ISettingsPropertyDiscoverer where TImplementation : class, TService
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((IGenericServiceFactory sp) => (TService)(object)sp.GetService<TImplementation>());
		services.AddSettingsPropertyDiscoverer<TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddSettingsPropertyDiscoverer<TImplementation>(this IGenericServiceContainer services) where TImplementation : class, ISettingsPropertyDiscoverer
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, ISettingsPropertyDiscoverer>)((IGenericServiceFactory sp) => sp.GetService<TImplementation>()));
		return services;
	}

	public static IGenericServiceContainer AddSettingsContainer<TService, TImplementation>(this IGenericServiceContainer services) where TService : class, ISettingsContainer where TImplementation : class, TService
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((IGenericServiceFactory sp) => (TService)(object)sp.GetService<TImplementation>());
		services.AddSettingsContainer<TImplementation>();
		return services;
	}

	public static IGenericServiceContainer AddSettingsContainer<TImplementation>(this IGenericServiceContainer services) where TImplementation : class, ISettingsContainer
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, ISettingsContainer>)((IGenericServiceFactory sp) => sp.GetService<TImplementation>()));
		if (typeof(IPerCampaignSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IPerCampaignSettingsContainer)sp.GetService<TImplementation>());
		}
		if (typeof(IPerSaveSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IPerSaveSettingsContainer)sp.GetService<TImplementation>());
		}
		if (typeof(IGlobalSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IGlobalSettingsContainer)sp.GetService<TImplementation>());
		}
		if (typeof(IFluentPerCampaignSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IFluentPerCampaignSettingsContainer)sp.GetService<TImplementation>());
		}
		if (typeof(IFluentPerSaveSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IFluentPerSaveSettingsContainer)sp.GetService<TImplementation>());
		}
		if (typeof(IFluentGlobalSettingsContainer).IsAssignableFrom(typeof(TImplementation)))
		{
			services.RegisterSingleton((IGenericServiceFactory sp) => (IFluentGlobalSettingsContainer)sp.GetService<TImplementation>());
		}
		return services;
	}

	public static IGenericServiceContainer AddExternalSettingsProvider<TImplementation>(this IGenericServiceContainer services) where TImplementation : class, IExternalSettingsProvider
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, IExternalSettingsProvider>)((IGenericServiceFactory sp) => sp.GetService<TImplementation>()));
		return services;
	}

	public static IGenericServiceContainer AddSettingsBuilderFactory<TImplementation>(this IGenericServiceContainer services) where TImplementation : class, ISettingsBuilderFactory
	{
		services.RegisterSingleton<TImplementation>();
		services.RegisterSingleton((Func<IGenericServiceFactory, ISettingsBuilderFactory>)((IGenericServiceFactory factory) => factory.GetService<TImplementation>()));
		return services;
	}
}
