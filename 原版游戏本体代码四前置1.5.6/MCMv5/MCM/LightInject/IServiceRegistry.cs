using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCM.LightInject;

internal interface IServiceRegistry
{
	IEnumerable<ServiceRegistration> AvailableServices { get; }

	IServiceRegistry Register(Type serviceType, Type implementingType);

	IServiceRegistry Register(Type serviceType, Type implementingType, ILifetime lifetime);

	IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName);

	IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName, ILifetime lifetime);

	IServiceRegistry Register<TService, TImplementation>() where TImplementation : TService;

	IServiceRegistry Register<TService, TImplementation>(ILifetime lifetime) where TImplementation : TService;

	IServiceRegistry Register<TService, TImplementation>(string serviceName) where TImplementation : TService;

	IServiceRegistry Register<TService, TImplementation>(string serviceName, ILifetime lifetime) where TImplementation : TService;

	IServiceRegistry RegisterInstance<TService>(TService instance);

	IServiceRegistry RegisterInstance<TService>(TService instance, string serviceName);

	IServiceRegistry RegisterInstance(Type serviceType, object instance);

	IServiceRegistry RegisterInstance(Type serviceType, object instance, string serviceName);

	IServiceRegistry Register<TService>();

	IServiceRegistry Register<TService>(ILifetime lifetime);

	IServiceRegistry Register(Type serviceType);

	IServiceRegistry Register(Type serviceType, ILifetime lifetime);

	IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory);

	IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory);

	IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory, string serviceName);

	IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory);

	IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory, string serviceName);

	IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory);

	IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory, string serviceName);

	IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory);

	IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory, string serviceName);

	IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, ILifetime lifetime);

	IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName);

	IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName, ILifetime lifetime);

	IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifetimeFactory);

	IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifeTimeFactory, Func<int, string> serviceNameFormatter);

	IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory);

	IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory, ILifetime lifetime);

	IServiceRegistry Register(ServiceRegistration serviceRegistration);

	IServiceRegistry RegisterAssembly(Assembly assembly);

	IServiceRegistry RegisterAssembly(Assembly assembly, Func<Type, Type, bool> shouldRegister);

	IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory);

	IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister);

	IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider);

	IServiceRegistry RegisterFrom<TCompositionRoot>() where TCompositionRoot : ICompositionRoot, new();

	IServiceRegistry RegisterFrom<TCompositionRoot>(TCompositionRoot compositionRoot) where TCompositionRoot : ICompositionRoot;

	IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, TDependency> factory);

	IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, object[], TDependency> factory);

	IServiceRegistry RegisterPropertyDependency<TDependency>(Func<IServiceFactory, PropertyInfo, TDependency> factory);

	IServiceRegistry RegisterAssembly(string searchPattern);

	IServiceRegistry Decorate(Type serviceType, Type decoratorType, Func<ServiceRegistration, bool> predicate);

	IServiceRegistry Decorate(Type serviceType, Type decoratorType);

	IServiceRegistry Decorate<TService, TDecorator>() where TDecorator : TService;

	IServiceRegistry Decorate<TService>(Func<IServiceFactory, TService, TService> factory);

	IServiceRegistry Decorate(DecoratorRegistration decoratorRegistration);

	IServiceRegistry Override(Func<ServiceRegistration, bool> serviceSelector, Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory);

	IServiceRegistry Initialize(Func<ServiceRegistration, bool> predicate, Action<IServiceFactory, object> processor);

	IServiceRegistry SetDefaultLifetime<T>() where T : ILifetime, new();
}
