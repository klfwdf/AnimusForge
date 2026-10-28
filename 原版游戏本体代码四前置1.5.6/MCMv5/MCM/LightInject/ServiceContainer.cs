using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ServiceContainer : IServiceContainer, IServiceRegistry, IServiceFactory, IDisposable
{
	private struct ClosedGenericCandidate(Type closedGenericImplementingType, ILifetime lifetime)
	{
		public Type ClosedGenericImplementingType { get; } = closedGenericImplementingType;

		public ILifetime Lifetime { get; } = lifetime;
	}

	private class Storage<T>
	{
		public T[] Items = Array.Empty<T>();

		private readonly object lockObject = new object();

		public int Add(T value)
		{
			int num = Array.IndexOf(Items, value);
			if (num == -1)
			{
				return TryAddValue(value);
			}
			return num;
		}

		private int TryAddValue(T value)
		{
			lock (lockObject)
			{
				int num = Array.IndexOf(Items, value);
				if (num == -1)
				{
					num = AddValue(value);
				}
				return num;
			}
		}

		private int AddValue(T value)
		{
			int num = Items.Length;
			T[] array = CreateSnapshot();
			array[num] = value;
			Items = array;
			return num;
		}

		private T[] CreateSnapshot()
		{
			T[] array = new T[Items.Length + 1];
			Array.Copy(Items, array, Items.Length);
			return array;
		}
	}

	private class PropertyDependencyDisabler : IPropertyDependencySelector
	{
		public IEnumerable<PropertyDependency> Execute(Type type)
		{
			return Array.Empty<PropertyDependency>();
		}
	}

	private class DynamicMethodSkeleton : IMethodSkeleton
	{
		private IEmitter emitter;

		private DynamicMethod dynamicMethod;

		public DynamicMethodSkeleton(Type returnType, Type[] parameterTypes)
		{
			CreateDynamicMethod(returnType, parameterTypes);
		}

		public IEmitter GetEmitter()
		{
			return emitter;
		}

		public Delegate CreateDelegate(Type delegateType)
		{
			return dynamicMethod.CreateDelegate(delegateType);
		}

		private void CreateDynamicMethod(Type returnType, Type[] parameterTypes)
		{
			dynamicMethod = new DynamicMethod(returnType, parameterTypes);
			emitter = new Emitter(dynamicMethod.GetILGenerator(), parameterTypes);
		}
	}

	private class ServiceRegistry<T> : ThreadSafeDictionary<Type, ThreadSafeDictionary<string, T>>
	{
	}

	private class FactoryRule
	{
		public Func<Type, string, bool> CanCreateInstance { get; set; }

		public Func<ServiceRequest, object> Factory { get; set; }

		public ILifetime LifeTime { get; set; }
	}

	private class Initializer
	{
		public Func<ServiceRegistration, bool> Predicate { get; set; }

		public Action<IServiceFactory, object> Initialize { get; set; }
	}

	private class ServiceOverride
	{
		private readonly object lockObject = new object();

		private readonly Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory;

		private bool hasExecuted;

		public Func<ServiceRegistration, bool> CanOverride { get; }

		public ServiceOverride(Func<ServiceRegistration, bool> canOverride, Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory)
		{
			CanOverride = canOverride;
			this.serviceRegistrationFactory = serviceRegistrationFactory;
		}

		[ExcludeFromCodeCoverage]
		public ServiceRegistration Execute(IServiceFactory serviceFactory, ServiceRegistration serviceRegistration)
		{
			if (hasExecuted)
			{
				return serviceRegistration;
			}
			lock (lockObject)
			{
				if (hasExecuted)
				{
					return serviceRegistration;
				}
				hasExecuted = true;
				return serviceRegistrationFactory(serviceFactory, serviceRegistration);
			}
		}
	}

	private const string UnresolvedDependencyError = "Unresolved dependency {0}";

	private static readonly MethodInfo OpenGenericTrackInstanceMethod = typeof(ServiceContainer).GetTypeInfo().GetDeclaredMethod("TrackInstance");

	private readonly Action<LogEntry> log;

	private readonly Func<Type, Type[], IMethodSkeleton> methodSkeletonFactory;

	private readonly ServiceRegistry<Action<IEmitter>> emitters = new ServiceRegistry<Action<IEmitter>>();

	private readonly ServiceRegistry<Delegate> constructorDependencyFactories = new ServiceRegistry<Delegate>();

	private readonly ServiceRegistry<Delegate> propertyDependencyFactories = new ServiceRegistry<Delegate>();

	private readonly ServiceRegistry<ServiceRegistration> availableServices = new ServiceRegistry<ServiceRegistration>();

	private readonly object lockObject = new object();

	private readonly ContainerOptions options;

	private readonly Storage<object> constants = new Storage<object>();

	private readonly Storage<ILifetime> disposableLifeTimes = new Storage<ILifetime>();

	private readonly Storage<DecoratorRegistration> decorators = new Storage<DecoratorRegistration>();

	private readonly Storage<ServiceOverride> overrides = new Storage<ServiceOverride>();

	private readonly Storage<FactoryRule> factoryRules = new Storage<FactoryRule>();

	private readonly Storage<Initializer> initializers = new Storage<Initializer>();

	private readonly Stack<Action<IEmitter>> dependencyStack = new Stack<Action<IEmitter>>();

	private readonly Lazy<IConstructionInfoProvider> constructionInfoProvider;

	private readonly List<IDisposable> disposableObjects = new List<IDisposable>();

	private readonly LazyConcurrentDictionary<ServiceRegistration, int> servicesToDelegatesIndex = new LazyConcurrentDictionary<ServiceRegistration, int>();

	private ImmutableHashTable<Type, GetInstanceDelegate> delegates = ImmutableHashTable<Type, GetInstanceDelegate>.Empty;

	private ImmutableHashTable<(Type, string), GetInstanceDelegate> namedDelegates = ImmutableHashTable<(Type, string), GetInstanceDelegate>.Empty;

	private ImmutableHashTree<Type, Func<object[], Scope, object, object>> propertyInjectionDelegates = ImmutableHashTree<Type, Func<object[], Scope, object, object>>.Empty;

	private bool isLocked;

	private Type defaultLifetimeType;

	public IScopeManagerProvider ScopeManagerProvider { get; set; }

	public IPropertyDependencySelector PropertyDependencySelector { get; set; }

	public ITypeExtractor CompositionRootTypeExtractor { get; set; }

	public IServiceNameProvider ServiceNameProvider { get; set; }

	public ICompositionRootExecutor CompositionRootExecutor { get; set; }

	public IConstructorDependencySelector ConstructorDependencySelector { get; set; }

	public IConstructorSelector ConstructorSelector { get; set; }

	public IGenericArgumentMapper GenericArgumentMapper { get; set; }

	public IAssemblyScanner AssemblyScanner { get; set; }

	public IAssemblyLoader AssemblyLoader { get; set; }

	public IEnumerable<ServiceRegistration> AvailableServices => availableServices.Values.SelectMany((ThreadSafeDictionary<string, ServiceRegistration> t) => t.Values);

	private ILifetime DefaultLifetime => (ILifetime)((defaultLifetimeType != null) ? Activator.CreateInstance(defaultLifetimeType) : null);

	public ServiceContainer()
		: this(ContainerOptions.Default)
	{
	}

	public ServiceContainer(Action<ContainerOptions> configureOptions)
	{
		options = new ContainerOptions();
		configureOptions(options);
		log = options.LogFactory(typeof(ServiceContainer));
		CachedTypeExtractor concreteTypeExtractor = new CachedTypeExtractor(new ConcreteTypeExtractor());
		CompositionRootTypeExtractor = new CachedTypeExtractor(new CompositionRootTypeExtractor(new CompositionRootAttributeExtractor()));
		CompositionRootExecutor = new CompositionRootExecutor(this, (Type type) => (ICompositionRoot)Activator.CreateInstance(type));
		ServiceNameProvider = new ServiceNameProvider();
		IPropertyDependencySelector propertyDependencySelector2;
		if (!options.EnablePropertyInjection)
		{
			IPropertyDependencySelector propertyDependencySelector = new PropertyDependencyDisabler();
			propertyDependencySelector2 = propertyDependencySelector;
		}
		else
		{
			IPropertyDependencySelector propertyDependencySelector = new PropertyDependencySelector(new PropertySelector());
			propertyDependencySelector2 = propertyDependencySelector;
		}
		PropertyDependencySelector = propertyDependencySelector2;
		GenericArgumentMapper = new GenericArgumentMapper();
		AssemblyScanner = new AssemblyScanner(concreteTypeExtractor, CompositionRootTypeExtractor, CompositionRootExecutor, GenericArgumentMapper);
		ConstructorDependencySelector = new ConstructorDependencySelector();
		ConstructorSelector = new MostResolvableConstructorSelector(CanGetInstance, options.EnableOptionalArguments);
		constructionInfoProvider = new Lazy<IConstructionInfoProvider>(CreateConstructionInfoProvider);
		methodSkeletonFactory = (Type returnType, Type[] parameterTypes) => new DynamicMethodSkeleton(returnType, parameterTypes);
		ScopeManagerProvider = new PerLogicalCallContextScopeManagerProvider();
		AssemblyLoader = new AssemblyLoader();
	}

	public ServiceContainer(ContainerOptions options)
		: this(delegate(ContainerOptions o)
		{
			o.LogFactory = options.LogFactory;
			o.DefaultServiceSelector = options.DefaultServiceSelector;
			o.EnableCurrentScope = options.EnableCurrentScope;
			o.EnablePropertyInjection = options.EnablePropertyInjection;
			o.EnableVariance = options.EnableVariance;
			o.VarianceFilter = options.VarianceFilter;
			o.EnableOptionalArguments = options.EnableOptionalArguments;
			o.OptimizeForLargeObjectGraphs = options.OptimizeForLargeObjectGraphs;
		})
	{
	}

	private ServiceContainer(ContainerOptions options, ServiceRegistry<Delegate> constructorDependencyFactories, ServiceRegistry<Delegate> propertyDependencyFactories, ServiceRegistry<ServiceRegistration> availableServices, Storage<DecoratorRegistration> decorators, Storage<ServiceOverride> overrides, Storage<FactoryRule> factoryRules, Storage<Initializer> initializers, Lazy<IConstructionInfoProvider> constructionInfoProvider, Func<Type, Type[], IMethodSkeleton> methodSkeletonFactory, Action<LogEntry> log, ICompositionRootExecutor compositionRootExecutor, IServiceNameProvider serviceNameProvider, IPropertyDependencySelector propertyDependencySelector, IGenericArgumentMapper genericArgumentMapper, IAssemblyScanner assemblyScanner, IConstructorDependencySelector constructorDependencySelector, IConstructorSelector constructorSelector, IAssemblyLoader assemblyLoader, IScopeManagerProvider scopeManagerProvider)
	{
		this.options = options;
		this.constructorDependencyFactories = constructorDependencyFactories;
		this.propertyDependencyFactories = propertyDependencyFactories;
		this.availableServices = availableServices;
		this.decorators = decorators;
		this.overrides = overrides;
		this.factoryRules = factoryRules;
		this.initializers = initializers;
		this.constructionInfoProvider = constructionInfoProvider;
		this.methodSkeletonFactory = methodSkeletonFactory;
		this.log = log;
		CompositionRootExecutor = compositionRootExecutor;
		ServiceNameProvider = serviceNameProvider;
		PropertyDependencySelector = propertyDependencySelector;
		GenericArgumentMapper = genericArgumentMapper;
		AssemblyScanner = assemblyScanner;
		ConstructorDependencySelector = constructorDependencySelector;
		ConstructorSelector = constructorSelector;
		ScopeManagerProvider = scopeManagerProvider;
		AssemblyLoader = assemblyLoader;
		foreach (ServiceRegistration availableService in AvailableServices)
		{
			Register(availableService);
		}
	}

	public bool CanGetInstance(Type serviceType, string serviceName)
	{
		if (serviceType.IsFuncRepresentingService() || serviceType.IsFuncRepresentingNamedService() || serviceType.IsFuncWithParameters() || serviceType.IsLazy())
		{
			Type serviceType2 = serviceType.GenericTypeArguments.Last();
			return GetEmitMethod(serviceType2, serviceName) != null || availableServices.ContainsKey(serviceType);
		}
		return GetEmitMethod(serviceType, serviceName) != null;
	}

	public Scope BeginScope()
	{
		if (options.EnableCurrentScope)
		{
			return ScopeManagerProvider.GetScopeManager(this).BeginScope();
		}
		return new Scope(this);
	}

	public object InjectProperties(object instance)
	{
		Type type = instance.GetType();
		Func<object[], Scope, object, object> func = propertyInjectionDelegates.Search(type);
		if (func == null)
		{
			func = CreatePropertyInjectionDelegate(type);
			propertyInjectionDelegates = propertyInjectionDelegates.Add(type, func);
		}
		return func(constants.Items, null, instance);
	}

	public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName, ILifetime lifetime)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, lifetime, serviceName);
		return this;
	}

	public IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory)
	{
		return RegisterFallback(predicate, factory, DefaultLifetime);
	}

	public IServiceRegistry RegisterFallback(Func<Type, string, bool> predicate, Func<ServiceRequest, object> factory, ILifetime lifetime)
	{
		factoryRules.Add(new FactoryRule
		{
			CanCreateInstance = predicate,
			Factory = factory,
			LifeTime = lifetime
		});
		return this;
	}

	public IServiceRegistry Register(ServiceRegistration serviceRegistration)
	{
		ThreadSafeDictionary<string, ServiceRegistration> threadSafeDictionary = GetAvailableServices(serviceRegistration.ServiceType);
		ServiceRegistration sr = serviceRegistration;
		threadSafeDictionary.AddOrUpdate(serviceRegistration.ServiceName, (string s) => AddServiceRegistration(sr), (string k, ServiceRegistration existing) => UpdateServiceRegistration(existing, sr));
		return this;
	}

	public IServiceRegistry RegisterAssembly(Assembly assembly)
	{
		Type[] array = CompositionRootTypeExtractor.Execute(assembly);
		if (array.Length == 0)
		{
			RegisterAssembly(assembly, (Type serviceType, Type implementingType) => true);
		}
		else
		{
			AssemblyScanner.Scan(assembly, this);
		}
		return this;
	}

	public IServiceRegistry RegisterAssembly(Assembly assembly, Func<Type, Type, bool> shouldRegister)
	{
		return RegisterAssembly(assembly, () => DefaultLifetime, shouldRegister);
	}

	public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory)
	{
		return RegisterAssembly(assembly, lifetimeFactory, (Type serviceType, Type implementingType) => true);
	}

	public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister)
	{
		return RegisterAssembly(assembly, lifetimeFactory, shouldRegister, ServiceNameProvider.GetServiceName);
	}

	public IServiceRegistry RegisterAssembly(Assembly assembly, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
	{
		AssemblyScanner.Scan(assembly, this, lifetimeFactory, shouldRegister, serviceNameProvider);
		return this;
	}

	public IServiceRegistry RegisterFrom<TCompositionRoot>() where TCompositionRoot : ICompositionRoot, new()
	{
		CompositionRootExecutor.Execute(typeof(TCompositionRoot));
		return this;
	}

	public IServiceRegistry RegisterFrom<TCompositionRoot>(TCompositionRoot compositionRoot) where TCompositionRoot : ICompositionRoot
	{
		CompositionRootExecutor.Execute(compositionRoot);
		return this;
	}

	public IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, TDependency> factory)
	{
		if (isLocked)
		{
			string message = $"Attempt to register a constructor dependency {typeof(TDependency)} after the first call to GetInstance." + $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";
			log.Warning(message);
		}
		GetConstructorDependencyFactories(typeof(TDependency)).AddOrUpdate(string.Empty, (string s) => factory, (string s, Delegate e) => isLocked ? e : factory);
		return this;
	}

	public IServiceRegistry RegisterConstructorDependency<TDependency>(Func<IServiceFactory, ParameterInfo, object[], TDependency> factory)
	{
		if (isLocked)
		{
			string message = $"Attempt to register a constructor dependency {typeof(TDependency)} after the first call to GetInstance." + $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";
			log.Warning(message);
		}
		GetConstructorDependencyFactories(typeof(TDependency)).AddOrUpdate(string.Empty, (string s) => factory, (string s, Delegate e) => isLocked ? e : factory);
		return this;
	}

	public IServiceRegistry RegisterPropertyDependency<TDependency>(Func<IServiceFactory, PropertyInfo, TDependency> factory)
	{
		if (isLocked)
		{
			string message = $"Attempt to register a property dependency {typeof(TDependency)} after the first call to GetInstance." + $"This might lead to incorrect behavior if a service with a {typeof(TDependency)} dependency has already been resolved";
			log.Warning(message);
		}
		GetPropertyDependencyFactories(typeof(TDependency)).AddOrUpdate(string.Empty, (string s) => factory, (string s, Delegate e) => isLocked ? e : factory);
		return this;
	}

	public IServiceRegistry RegisterAssembly(string searchPattern)
	{
		foreach (Assembly item in AssemblyLoader.Load(searchPattern))
		{
			RegisterAssembly(item);
		}
		return this;
	}

	public IServiceRegistry Decorate(Type serviceType, Type decoratorType, Func<ServiceRegistration, bool> predicate)
	{
		DecoratorRegistration decoratorRegistration = new DecoratorRegistration
		{
			ServiceType = serviceType,
			ImplementingType = decoratorType,
			CanDecorate = predicate
		};
		Decorate(decoratorRegistration);
		return this;
	}

	public IServiceRegistry Decorate(Type serviceType, Type decoratorType)
	{
		Decorate(serviceType, decoratorType, (ServiceRegistration si) => true);
		return this;
	}

	public IServiceRegistry Decorate<TService, TDecorator>() where TDecorator : TService
	{
		Decorate(typeof(TService), typeof(TDecorator));
		return this;
	}

	public IServiceRegistry Decorate<TService>(Func<IServiceFactory, TService, TService> factory)
	{
		DecoratorRegistration decoratorRegistration = new DecoratorRegistration();
		decoratorRegistration.FactoryExpression = factory;
		decoratorRegistration.ServiceType = typeof(TService);
		decoratorRegistration.CanDecorate = (ServiceRegistration si) => true;
		DecoratorRegistration decoratorRegistration2 = decoratorRegistration;
		Decorate(decoratorRegistration2);
		return this;
	}

	public IServiceRegistry Decorate(DecoratorRegistration decoratorRegistration)
	{
		int index = decorators.Add(decoratorRegistration);
		decoratorRegistration.Index = index;
		return this;
	}

	public IServiceRegistry Override(Func<ServiceRegistration, bool> serviceSelector, Func<IServiceFactory, ServiceRegistration, ServiceRegistration> serviceRegistrationFactory)
	{
		ServiceOverride value = new ServiceOverride(serviceSelector, serviceRegistrationFactory);
		overrides.Add(value);
		return this;
	}

	public IServiceRegistry Initialize(Func<ServiceRegistration, bool> predicate, Action<IServiceFactory, object> processor)
	{
		initializers.Add(new Initializer
		{
			Predicate = predicate,
			Initialize = processor
		});
		return this;
	}

	public IServiceRegistry Register(Type serviceType, Type implementingType, ILifetime lifetime)
	{
		Register(serviceType, implementingType, string.Empty, lifetime);
		return this;
	}

	public IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName, ILifetime lifetime)
	{
		RegisterService(serviceType, implementingType, lifetime, serviceName);
		return this;
	}

	public IServiceRegistry Register<TService, TImplementation>() where TImplementation : TService
	{
		Register(typeof(TService), typeof(TImplementation));
		return this;
	}

	public IServiceRegistry Register<TService, TImplementation>(ILifetime lifetime) where TImplementation : TService
	{
		Register(typeof(TService), typeof(TImplementation), lifetime);
		return this;
	}

	public IServiceRegistry Register<TService, TImplementation>(string serviceName) where TImplementation : TService
	{
		Register<TService, TImplementation>(serviceName, null);
		return this;
	}

	public IServiceRegistry Register<TService, TImplementation>(string serviceName, ILifetime lifetime) where TImplementation : TService
	{
		Register(typeof(TService), typeof(TImplementation), serviceName, lifetime);
		return this;
	}

	public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, ILifetime lifetime)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, lifetime, string.Empty);
		return this;
	}

	public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory, string serviceName)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
		return this;
	}

	public IServiceRegistry Register<TService>()
	{
		Register<TService, TService>();
		return this;
	}

	public IServiceRegistry Register(Type serviceType)
	{
		Register(serviceType, serviceType);
		return this;
	}

	public IServiceRegistry Register(Type serviceType, ILifetime lifetime)
	{
		Register(serviceType, serviceType, lifetime);
		return this;
	}

	public IServiceRegistry Register<TService>(ILifetime lifetime)
	{
		Register<TService, TService>(lifetime);
		return this;
	}

	public IServiceRegistry RegisterInstance<TService>(TService instance, string serviceName)
	{
		RegisterInstance(typeof(TService), instance, serviceName);
		return this;
	}

	public IServiceRegistry RegisterInstance<TService>(TService instance)
	{
		RegisterInstance(typeof(TService), instance);
		return this;
	}

	public IServiceRegistry RegisterInstance(Type serviceType, object instance)
	{
		RegisterInstance(serviceType, instance, string.Empty);
		return this;
	}

	public IServiceRegistry RegisterInstance(Type serviceType, object instance, string serviceName)
	{
		Ensure.IsNotNull(instance, "instance");
		Ensure.IsNotNull(serviceType, "type");
		Ensure.IsNotNull(serviceName, "serviceName");
		RegisterValue(serviceType, instance, serviceName);
		return this;
	}

	public IServiceRegistry Register<TService>(Func<IServiceFactory, TService> factory)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
		return this;
	}

	public IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
		return this;
	}

	public IServiceRegistry Register<T, TService>(Func<IServiceFactory, T, TService> factory, string serviceName)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
		return this;
	}

	public IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
		return this;
	}

	public IServiceRegistry Register<T1, T2, TService>(Func<IServiceFactory, T1, T2, TService> factory, string serviceName)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
		return this;
	}

	public IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
		return this;
	}

	public IServiceRegistry Register<T1, T2, T3, TService>(Func<IServiceFactory, T1, T2, T3, TService> factory, string serviceName)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
		return this;
	}

	public IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, string.Empty);
		return this;
	}

	public IServiceRegistry Register<T1, T2, T3, T4, TService>(Func<IServiceFactory, T1, T2, T3, T4, TService> factory, string serviceName)
	{
		RegisterServiceFromLambdaExpression<TService>(factory, null, serviceName);
		return this;
	}

	public IServiceRegistry Register(Type serviceType, Type implementingType, string serviceName)
	{
		RegisterService(serviceType, implementingType, null, serviceName);
		return this;
	}

	public IServiceRegistry Register(Type serviceType, Type implementingType)
	{
		RegisterService(serviceType, implementingType, null, string.Empty);
		return this;
	}

	public IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifeTimeFactory)
	{
		return RegisterOrdered(serviceType, implementingTypes, lifeTimeFactory, (int i) => i.ToString().PadLeft(3, '0'));
	}

	public IServiceRegistry RegisterOrdered(Type serviceType, Type[] implementingTypes, Func<Type, ILifetime> lifeTimeFactory, Func<int, string> serviceNameFormatter)
	{
		int num = GetAvailableServices(serviceType).Count;
		foreach (Type type in implementingTypes)
		{
			num++;
			Register(serviceType, type, serviceNameFormatter(num), lifeTimeFactory(type));
		}
		return this;
	}

	public void Compile(Func<ServiceRegistration, bool> predicate)
	{
		ServiceRegistration[] array = AvailableServices.Where(predicate).ToArray();
		ServiceRegistration[] array2 = array;
		foreach (ServiceRegistration serviceRegistration in array2)
		{
			if (serviceRegistration.ServiceType.GetTypeInfo().IsGenericTypeDefinition)
			{
				log.Warning("Unable to precompile open generic type '" + GetPrettyName(serviceRegistration.ServiceType) + "'");
			}
			else if (string.IsNullOrWhiteSpace(serviceRegistration.ServiceName))
			{
				CreateDefaultDelegate(serviceRegistration.ServiceType, throwError: true);
			}
			else
			{
				CreateNamedDelegate((serviceRegistration.ServiceType, serviceRegistration.ServiceName), throwError: true);
			}
		}
		static string GetPrettyName(Type type)
		{
			if (type.GetTypeInfo().IsGenericType)
			{
				return type.FullName.Substring(0, type.FullName.LastIndexOf("`", StringComparison.OrdinalIgnoreCase)) + "<" + string.Join(", ", type.GetTypeInfo().GenericTypeParameters.Select(GetPrettyName)) + ">";
			}
			return type.Name;
		}
	}

	public void Compile()
	{
		Compile((ServiceRegistration sr) => true);
	}

	public void Compile<TService>(string serviceName = null)
	{
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			CreateDefaultDelegate(typeof(TService), throwError: true);
		}
		else
		{
			CreateNamedDelegate((typeof(TService), serviceName), throwError: true);
		}
	}

	public object GetInstance(Type serviceType)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
		}
		return getInstanceDelegate(constants.Items, null);
	}

	public object GetInstance(Type serviceType, object[] arguments)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
		}
		object[] args = constants.Items.Concat(new object[1] { arguments }).ToArray();
		return getInstanceDelegate(args, null);
	}

	public object GetInstance(Type serviceType, string serviceName, object[] arguments)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: true);
		}
		object[] args = constants.Items.Concat(new object[1] { arguments }).ToArray();
		return getInstanceDelegate(args, null);
	}

	public object TryGetInstance(Type serviceType)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: false);
		}
		return getInstanceDelegate(constants.Items, null);
	}

	public object TryGetInstance(Type serviceType, string serviceName)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: false);
		}
		return getInstanceDelegate(constants.Items, null);
	}

	public object GetInstance(Type serviceType, string serviceName)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: true);
		}
		return getInstanceDelegate(constants.Items, null);
	}

	public IEnumerable<object> GetAllInstances(Type serviceType)
	{
		return (IEnumerable<object>)GetInstance(serviceType.GetEnumerableType());
	}

	public object Create(Type serviceType)
	{
		Register(serviceType);
		return GetInstance(serviceType);
	}

	public IServiceRegistry SetDefaultLifetime<T>() where T : ILifetime, new()
	{
		defaultLifetimeType = typeof(T);
		return this;
	}

	public void Dispose()
	{
		IEnumerable<IDisposable> enumerable = disposableLifeTimes.Items.Where((ILifetime lt) => lt is IDisposable).Cast<IDisposable>().Reverse();
		foreach (IDisposable item in enumerable)
		{
			item.Dispose();
		}
		IEnumerable<IDisposable> enumerable2 = disposableObjects.AsEnumerable().Reverse();
		foreach (IDisposable item2 in enumerable2)
		{
			item2.Dispose();
		}
	}

	public ServiceContainer Clone()
	{
		return new ServiceContainer(options, constructorDependencyFactories, propertyDependencyFactories, availableServices, decorators, overrides, factoryRules, initializers, constructionInfoProvider, methodSkeletonFactory, log, CompositionRootExecutor, ServiceNameProvider, PropertyDependencySelector, GenericArgumentMapper, AssemblyScanner, ConstructorDependencySelector, ConstructorSelector, AssemblyLoader, ScopeManagerProvider);
	}

	internal static TService TrackInstance<TService>(TService instance, ServiceContainer container)
	{
		if (instance is IDisposable item)
		{
			container.disposableObjects.Add(item);
		}
		return instance;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	internal object GetInstance(Type serviceType, Scope scope)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
		}
		return getInstanceDelegate(constants.Items, scope);
	}

	internal object GetInstance(Type serviceType, Scope scope, string serviceName)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: true);
		}
		return getInstanceDelegate(constants.Items, scope);
	}

	internal IEnumerable<object> GetAllInstances(Type serviceType, Scope scope)
	{
		return (IEnumerable<object>)GetInstance(serviceType.GetEnumerableType(), scope);
	}

	internal object GetInstance(Type serviceType, object[] arguments, Scope scope)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: true);
		}
		object[] args = constants.Items.Concat(new object[1] { arguments }).ToArray();
		return getInstanceDelegate(args, scope);
	}

	internal object GetInstance(Type serviceType, string serviceName, object[] arguments, Scope scope)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: true);
		}
		object[] args = constants.Items.Concat(new object[1] { arguments }).ToArray();
		return getInstanceDelegate(args, scope);
	}

	internal object TryGetInstance(Type serviceType, Scope scope)
	{
		GetInstanceDelegate getInstanceDelegate = delegates.Search(serviceType);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateDefaultDelegate(serviceType, throwError: false);
		}
		return getInstanceDelegate(constants.Items, scope);
	}

	internal object TryGetInstance(Type serviceType, string serviceName, Scope scope)
	{
		(Type, string) key = (serviceType, serviceName);
		GetInstanceDelegate getInstanceDelegate = namedDelegates.Search(key);
		if (getInstanceDelegate == null)
		{
			getInstanceDelegate = CreateNamedDelegate(key, throwError: false);
		}
		return getInstanceDelegate(constants.Items, scope);
	}

	internal object Create(Type serviceType, Scope scope)
	{
		Register(serviceType);
		return GetInstance(serviceType, scope);
	}

	private static void EmitEnumerable(IList<Action<IEmitter>> serviceEmitters, Type elementType, IEmitter emitter)
	{
		EmitNewArray(serviceEmitters, elementType, emitter);
	}

	private static void EmitNewArray(IList<Action<IEmitter>> emitMethods, Type elementType, IEmitter emitter)
	{
		LocalBuilder localBuilder = emitter.DeclareLocal(elementType.MakeArrayType());
		emitter.Push(emitMethods.Count);
		emitter.PushNewArray(elementType);
		emitter.Store(localBuilder);
		for (int i = 0; i < emitMethods.Count; i++)
		{
			emitter.Push(localBuilder);
			emitter.Push(i);
			emitMethods[i](emitter);
			emitter.UnboxOrCast(elementType);
			emitter.Emit(OpCodes.Stelem, elementType);
		}
		emitter.Push(localBuilder);
	}

	private static ILifetime CloneLifeTime(ILifetime lifetime)
	{
		if (lifetime is ICloneableLifeTime cloneableLifeTime)
		{
			return cloneableLifeTime.Clone();
		}
		return (lifetime == null) ? null : ((ILifetime)Activator.CreateInstance(lifetime.GetType()));
	}

	private static ConstructorDependency GetConstructorDependencyThatRepresentsDecoratorTarget(DecoratorRegistration decoratorRegistration, ConstructionInfo constructionInfo)
	{
		return constructionInfo.ConstructorDependencies.FirstOrDefault((ConstructorDependency cd) => cd.ServiceType == decoratorRegistration.ServiceType || (cd.ServiceType.IsLazy() && cd.ServiceType.GetTypeInfo().GenericTypeArguments[0] == decoratorRegistration.ServiceType));
	}

	private static void PushRuntimeArguments(IEmitter emitter)
	{
		MethodInfo declaredMethod = typeof(RuntimeArgumentsLoader).GetTypeInfo().GetDeclaredMethod("Load");
		emitter.Emit(OpCodes.Ldarg_0);
		emitter.Emit(OpCodes.Call, declaredMethod);
	}

	private DecoratorRegistration CreateClosedGenericDecoratorRegistration(ServiceRegistration serviceRegistration, DecoratorRegistration openGenericDecorator)
	{
		Type implementingType = openGenericDecorator.ImplementingType;
		Type serviceType = serviceRegistration.ServiceType;
		Type[] genericTypeArguments = serviceType.GenericTypeArguments;
		if (!TryCreateClosedGenericDecoratorType(serviceType, implementingType, out var closedGenericDecoratorType))
		{
			log.Info("Skipping decorator [" + implementingType.FullName + "] since it is incompatible with the service type [" + serviceType.FullName + "]");
			return null;
		}
		return new DecoratorRegistration
		{
			ServiceType = serviceRegistration.ServiceType,
			ImplementingType = closedGenericDecoratorType,
			CanDecorate = openGenericDecorator.CanDecorate,
			Index = openGenericDecorator.Index
		};
	}

	private bool TryCreateClosedGenericDecoratorType(Type serviceType, Type implementingType, out Type closedGenericDecoratorType)
	{
		closedGenericDecoratorType = null;
		GenericMappingResult genericMappingResult = GenericArgumentMapper.Map(serviceType, implementingType);
		if (!genericMappingResult.IsValid)
		{
			return false;
		}
		closedGenericDecoratorType = TypeHelper.TryMakeGenericType(implementingType, genericMappingResult.GetMappedArguments());
		if (closedGenericDecoratorType == null)
		{
			return false;
		}
		if (!serviceType.GetTypeInfo().IsAssignableFrom(closedGenericDecoratorType.GetTypeInfo()))
		{
			return false;
		}
		return true;
	}

	private Func<object[], Scope, object, object> CreatePropertyInjectionDelegate(Type concreteType)
	{
		lock (lockObject)
		{
			IMethodSkeleton methodSkeleton = methodSkeletonFactory(typeof(object), new Type[3]
			{
				typeof(object[]),
				typeof(Scope),
				typeof(object)
			});
			ConstructionInfo constructionInfo = new ConstructionInfo();
			constructionInfo.PropertyDependencies.AddRange(PropertyDependencySelector.Execute(concreteType));
			constructionInfo.ImplementingType = concreteType;
			IEmitter emitter = methodSkeleton.GetEmitter();
			emitter.PushArgument(2);
			emitter.Cast(concreteType);
			try
			{
				EmitPropertyDependencies(constructionInfo, emitter);
			}
			catch (Exception)
			{
				dependencyStack.Clear();
				throw;
			}
			emitter.Return();
			isLocked = true;
			return (Func<object[], Scope, object, object>)methodSkeleton.CreateDelegate(typeof(Func<object[], Scope, object, object>));
		}
	}

	private ConstructionInfoProvider CreateConstructionInfoProvider()
	{
		return new ConstructionInfoProvider(CreateTypeConstructionInfoBuilder());
	}

	private TypeConstructionInfoBuilder CreateTypeConstructionInfoBuilder()
	{
		return new TypeConstructionInfoBuilder(ConstructorSelector, ConstructorDependencySelector, PropertyDependencySelector, GetConstructorDependencyDelegate, GetPropertyDependencyExpression);
	}

	private Delegate GetConstructorDependencyDelegate(Type type, string serviceName)
	{
		GetConstructorDependencyFactories(type).TryGetValue(serviceName, out var value);
		return value;
	}

	private Delegate GetPropertyDependencyExpression(Type type, string serviceName)
	{
		GetPropertyDependencyFactories(type).TryGetValue(serviceName, out var value);
		return value;
	}

	private GetInstanceDelegate CreateDynamicMethodDelegate(Action<IEmitter> serviceEmitter)
	{
		IMethodSkeleton methodSkeleton = methodSkeletonFactory(typeof(object), new Type[2]
		{
			typeof(object[]),
			typeof(Scope)
		});
		IEmitter emitter = methodSkeleton.GetEmitter();
		serviceEmitter(emitter);
		if (emitter.StackType.GetTypeInfo().IsValueType)
		{
			emitter.Emit(OpCodes.Box, emitter.StackType);
		}
		Instruction instruction = emitter.Instructions.Last();
		if (instruction.Code == OpCodes.Castclass)
		{
			emitter.Instructions.Remove(instruction);
		}
		emitter.Return();
		isLocked = true;
		return (GetInstanceDelegate)methodSkeleton.CreateDelegate(typeof(GetInstanceDelegate));
	}

	private Action<IEmitter> GetEmitMethod(Type serviceType, string serviceName)
	{
		Action<IEmitter> action = GetRegisteredEmitMethod(serviceType, serviceName);
		if (action == null)
		{
			action = TryGetFallbackEmitMethod(serviceType, serviceName);
		}
		if (action == null)
		{
			AssemblyScanner.Scan(serviceType.GetTypeInfo().Assembly, this);
			action = GetRegisteredEmitMethod(serviceType, serviceName);
		}
		if (action == null)
		{
			action = TryGetFallbackEmitMethod(serviceType, serviceName);
		}
		return CreateEmitMethodWrapper(action, serviceType, serviceName);
	}

	private Action<IEmitter> TryGetFallbackEmitMethod(Type serviceType, string serviceName)
	{
		Action<IEmitter> action = null;
		FactoryRule factoryRule = factoryRules.Items.FirstOrDefault((FactoryRule r) => r.CanCreateInstance(serviceType, serviceName));
		if (factoryRule != null)
		{
			action = CreateServiceEmitterBasedOnFactoryRule(factoryRule, serviceType, serviceName);
			RegisterEmitMethod(serviceType, serviceName, action);
		}
		return action;
	}

	private Action<IEmitter> CreateEmitMethodWrapper(Action<IEmitter> emitter, Type serviceType, string serviceName)
	{
		if (emitter == null)
		{
			return null;
		}
		return delegate(IEmitter ms)
		{
			if (dependencyStack.Contains(emitter))
			{
				throw new InvalidOperationException($"Recursive dependency detected: ServiceType:{serviceType}, ServiceName:{serviceName}]");
			}
			dependencyStack.Push(emitter);
			try
			{
				emitter(ms);
			}
			finally
			{
				if (dependencyStack.Count > 0)
				{
					dependencyStack.Pop();
				}
			}
		};
	}

	private Action<IEmitter> GetRegisteredEmitMethod(Type serviceType, string serviceName)
	{
		ThreadSafeDictionary<string, Action<IEmitter>> emitMethods = GetEmitMethods(serviceType);
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			if (emitMethods.Count > 1)
			{
				string[] arg = emitMethods.Keys.OrderBy((string k) => k).ToArray();
				serviceName = options.DefaultServiceSelector(arg);
			}
			else
			{
				serviceName = string.Empty;
			}
		}
		emitMethods.TryGetValue(serviceName, out var value);
		return value ?? CreateEmitMethodForUnknownService(serviceType, serviceName);
	}

	private ServiceRegistration AddServiceRegistration(ServiceRegistration serviceRegistration)
	{
		Action<IEmitter> emitMethod = ResolveEmitMethod(serviceRegistration);
		RegisterEmitMethod(serviceRegistration.ServiceType, serviceRegistration.ServiceName, emitMethod);
		return serviceRegistration;
	}

	private void RegisterEmitMethod(Type serviceType, string serviceName, Action<IEmitter> emitMethod)
	{
		GetEmitMethods(serviceType).TryAdd(serviceName, emitMethod);
	}

	private ServiceRegistration UpdateServiceRegistration(ServiceRegistration existingRegistration, ServiceRegistration newRegistration)
	{
		if (isLocked)
		{
			string message = $"Cannot overwrite existing serviceregistration {existingRegistration} after the first call to GetInstance.";
			log.Warning(message);
			return existingRegistration;
		}
		Action<IEmitter> value = ResolveEmitMethod(newRegistration);
		ThreadSafeDictionary<string, Action<IEmitter>> emitMethods = GetEmitMethods(newRegistration.ServiceType);
		emitMethods[newRegistration.ServiceName] = value;
		return newRegistration;
	}

	private DecoratorRegistration[] GetDecorators(ServiceRegistration serviceRegistration)
	{
		List<DecoratorRegistration> list = decorators.Items.Where((DecoratorRegistration d) => d.ServiceType == serviceRegistration.ServiceType).ToList();
		list.AddRange(GetOpenGenericDecoratorRegistrations(serviceRegistration));
		list.AddRange(GetDeferredDecoratorRegistrations(serviceRegistration));
		return list.OrderBy((DecoratorRegistration d) => d.Index).ToArray();
	}

	private IEnumerable<DecoratorRegistration> GetOpenGenericDecoratorRegistrations(ServiceRegistration serviceRegistration)
	{
		List<DecoratorRegistration> list = new List<DecoratorRegistration>();
		TypeInfo typeInfo = serviceRegistration.ServiceType.GetTypeInfo();
		if (typeInfo.IsGenericType)
		{
			Type openGenericServiceType = typeInfo.GetGenericTypeDefinition();
			IEnumerable<DecoratorRegistration> source = decorators.Items.Where((DecoratorRegistration d) => d.ServiceType == openGenericServiceType);
			list.AddRange(from openGenericDecorator in source
				select CreateClosedGenericDecoratorRegistration(serviceRegistration, openGenericDecorator) into dr
				where dr != null
				select dr);
		}
		return list;
	}

	private IEnumerable<DecoratorRegistration> GetDeferredDecoratorRegistrations(ServiceRegistration serviceRegistration)
	{
		List<DecoratorRegistration> list = new List<DecoratorRegistration>();
		IEnumerable<DecoratorRegistration> enumerable = decorators.Items.Where((DecoratorRegistration ds) => ds.CanDecorate(serviceRegistration) && ds.HasDeferredImplementingType);
		foreach (DecoratorRegistration item2 in enumerable)
		{
			DecoratorRegistration item = new DecoratorRegistration
			{
				ServiceType = serviceRegistration.ServiceType,
				ImplementingType = item2.ImplementingTypeFactory(this, serviceRegistration),
				CanDecorate = (ServiceRegistration sr) => true,
				Index = item2.Index
			};
			list.Add(item);
		}
		return list;
	}

	private void EmitNewDecoratorInstance(DecoratorRegistration decoratorRegistration, IEmitter emitter, Action<IEmitter> pushInstance)
	{
		ConstructionInfo constructionInfo = GetConstructionInfo(decoratorRegistration);
		ConstructorDependency constructorDependencyThatRepresentsDecoratorTarget = GetConstructorDependencyThatRepresentsDecoratorTarget(decoratorRegistration, constructionInfo);
		if (constructorDependencyThatRepresentsDecoratorTarget != null)
		{
			constructorDependencyThatRepresentsDecoratorTarget.IsDecoratorTarget = true;
		}
		if ((object)constructionInfo.FactoryDelegate != null)
		{
			EmitNewDecoratorUsingFactoryDelegate(constructionInfo.FactoryDelegate, emitter, pushInstance);
		}
		else
		{
			EmitNewInstanceUsingImplementingType(emitter, constructionInfo, pushInstance);
		}
	}

	private void EmitNewDecoratorUsingFactoryDelegate(Delegate factoryDelegate, IEmitter emitter, Action<IEmitter> pushInstance)
	{
		int index = constants.Add(factoryDelegate);
		Type type = factoryDelegate.GetType();
		emitter.PushConstant(index, type);
		int index2 = constants.Add(this);
		emitter.PushConstant(index2, typeof(IServiceFactory));
		int index3 = CreateScopeManagerIndex();
		emitter.PushConstant(index3, typeof(IScopeManager));
		emitter.PushArgument(1);
		emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
		pushInstance(emitter);
		MethodInfo declaredMethod = type.GetTypeInfo().GetDeclaredMethod("Invoke");
		emitter.Emit(OpCodes.Callvirt, declaredMethod);
	}

	private void EmitNewInstance(ServiceRegistration serviceRegistration, IEmitter emitter)
	{
		if (serviceRegistration.Value != null)
		{
			int index = constants.Add(serviceRegistration.Value);
			Type serviceType = serviceRegistration.ServiceType;
			emitter.PushConstant(index, serviceType);
			return;
		}
		ConstructionInfo constructionInfo = GetConstructionInfo(serviceRegistration);
		if ((object)serviceRegistration.FactoryExpression != null)
		{
			EmitNewInstanceUsingFactoryDelegate(serviceRegistration, emitter);
		}
		else
		{
			EmitNewInstanceUsingImplementingType(emitter, constructionInfo, null);
		}
	}

	private void EmitDecorators(ServiceRegistration serviceRegistration, IEnumerable<DecoratorRegistration> serviceDecorators, IEmitter emitter, Action<IEmitter> decoratorTargetEmitMethod)
	{
		foreach (DecoratorRegistration serviceDecorator in serviceDecorators)
		{
			if (serviceDecorator.CanDecorate(serviceRegistration))
			{
				Action<IEmitter> currentDecoratorTargetEmitter = decoratorTargetEmitMethod;
				DecoratorRegistration currentDecorator = serviceDecorator;
				decoratorTargetEmitMethod = delegate(IEmitter e)
				{
					EmitNewDecoratorInstance(currentDecorator, e, currentDecoratorTargetEmitter);
				};
			}
		}
		decoratorTargetEmitMethod(emitter);
	}

	private void EmitNewInstanceUsingImplementingType(IEmitter emitter, ConstructionInfo constructionInfo, Action<IEmitter> decoratorTargetEmitMethod)
	{
		EmitConstructorDependencies(constructionInfo, emitter, decoratorTargetEmitMethod);
		emitter.Emit(OpCodes.Newobj, constructionInfo.Constructor);
		EmitPropertyDependencies(constructionInfo, emitter);
	}

	private void EmitNewInstanceUsingFactoryDelegate(ServiceRegistration serviceRegistration, IEmitter emitter)
	{
		int index = constants.Add(serviceRegistration.FactoryExpression);
		Type type = serviceRegistration.FactoryExpression.GetType();
		MethodInfo declaredMethod = type.GetTypeInfo().GetDeclaredMethod("Invoke");
		emitter.PushConstant(index, type);
		ParameterInfo[] parameters = declaredMethod.GetParameters();
		if (parameters.Length == 1 && parameters[0].ParameterType == typeof(ServiceRequest))
		{
			MethodInfo methodInfo = ServiceRequestHelper.CreateServiceRequestMethod.MakeGenericMethod(serviceRegistration.ServiceType);
			emitter.Emit(OpCodes.Ldstr, serviceRegistration.ServiceName);
			int index2 = constants.Add(this);
			emitter.PushConstant(index2, typeof(IServiceFactory));
			int index3 = CreateScopeManagerIndex();
			emitter.PushConstant(index3, typeof(IScopeManager));
			emitter.PushArgument(1);
			emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
			emitter.Emit(OpCodes.Call, methodInfo);
			emitter.Call(declaredMethod);
			emitter.UnboxOrCast(serviceRegistration.ServiceType);
		}
		else
		{
			int index4 = constants.Add(this);
			emitter.PushConstant(index4, typeof(IServiceFactory));
			int index5 = CreateScopeManagerIndex();
			emitter.PushConstant(index5, typeof(IScopeManager));
			emitter.PushArgument(1);
			emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
			if (parameters.Length > 1)
			{
				emitter.PushArguments(parameters.Skip(1).ToArray());
			}
			emitter.Call(declaredMethod);
		}
	}

	private void EmitConstructorDependencies(ConstructionInfo constructionInfo, IEmitter emitter, Action<IEmitter> decoratorTargetEmitter)
	{
		foreach (ConstructorDependency constructorDependency in constructionInfo.ConstructorDependencies)
		{
			if (!constructorDependency.IsDecoratorTarget)
			{
				EmitConstructorDependency(emitter, constructorDependency);
			}
			else if (constructorDependency.ServiceType.IsLazy())
			{
				LocalBuilder localBuilder = emitter.DeclareLocal(typeof(Scope));
				emitter.PushArgument(1);
				int index = CreateScopeManagerIndex();
				emitter.PushConstant(index, typeof(IScopeManager));
				emitter.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
				emitter.Store(localBuilder);
				int index2 = CreateInstanceDelegateIndex(decoratorTargetEmitter);
				emitter.PushConstant(index2, typeof(GetInstanceDelegate));
				emitter.PushArgument(0);
				emitter.Push(localBuilder);
				MethodInfo methodInfo = LazyHelper.CreateScopedLazyFromDelegateMethod.MakeGenericMethod(constructorDependency.ServiceType.GetTypeInfo().GenericTypeArguments.Last());
				emitter.Emit(OpCodes.Call, methodInfo);
			}
			else
			{
				decoratorTargetEmitter(emitter);
			}
		}
	}

	private void EmitConstructorDependency(IEmitter emitter, Dependency dependency)
	{
		Action<IEmitter> emitMethodForDependency = GetEmitMethodForDependency(dependency);
		try
		{
			emitMethodForDependency(emitter);
			emitter.UnboxOrCast(dependency.ServiceType);
		}
		catch (InvalidOperationException innerException)
		{
			throw new InvalidOperationException($"Unresolved dependency {dependency}", innerException);
		}
	}

	private void EmitPropertyDependency(IEmitter emitter, PropertyDependency propertyDependency, LocalBuilder instanceVariable)
	{
		Action<IEmitter> emitMethodForDependency = GetEmitMethodForDependency(propertyDependency);
		if (emitMethodForDependency != null)
		{
			emitter.Push(instanceVariable);
			emitMethodForDependency(emitter);
			emitter.UnboxOrCast(propertyDependency.ServiceType);
			emitter.Call(propertyDependency.Property.SetMethod);
		}
	}

	private Action<IEmitter> GetEmitMethodForDependency(Dependency dependency)
	{
		if ((object)dependency.FactoryExpression != null)
		{
			return delegate(IEmitter skeleton)
			{
				EmitDependencyUsingFactoryExpression(skeleton, dependency);
			};
		}
		Action<IEmitter> value = null;
		string key = (string.IsNullOrWhiteSpace(dependency.ServiceName) ? dependency.Name : dependency.ServiceName);
		ThreadSafeDictionary<string, Action<IEmitter>> emitMethods = GetEmitMethods(dependency.ServiceType);
		if (emitMethods.Count > 1 && emitMethods.TryGetValue(key, out value))
		{
			return value;
		}
		value = GetEmitMethod(dependency.ServiceType, dependency.ServiceName);
		if (value == null)
		{
			value = GetEmitMethod(dependency.ServiceType, dependency.Name);
			if (value == null && dependency.IsRequired)
			{
				if (!(dependency is ConstructorDependency constructorDependency) || !constructorDependency.Parameter.HasDefaultValue || !options.EnableOptionalArguments)
				{
					throw new InvalidOperationException($"Unresolved dependency {dependency}");
				}
				value = GetEmitMethodForDefaultValue(constructorDependency);
			}
		}
		return value;
	}

	private Action<IEmitter> GetEmitMethodForDefaultValue(ConstructorDependency constructorDependency)
	{
		Type parameterType = constructorDependency.Parameter.ParameterType;
		return delegate(IEmitter emitter)
		{
			object defaultValue = constructorDependency.Parameter.DefaultValue;
			if (defaultValue == null)
			{
				defaultValue = TypeHelper.GetDefaultValue(parameterType);
			}
			emitter.PushConstantValue(defaultValue, parameterType);
		};
	}

	private void EmitDependencyUsingFactoryExpression(IEmitter emitter, Dependency dependency)
	{
		List<Action<IEmitter>> list = new List<Action<IEmitter>>();
		ParameterInfo[] parameters = dependency.FactoryExpression.GetMethodInfo().GetParameters();
		ParameterInfo[] array = parameters;
		foreach (ParameterInfo parameterInfo in array)
		{
			if (parameterInfo.ParameterType == typeof(IServiceFactory))
			{
				int serviceFactoryIndex = constants.Add(this);
				int scopeManagerIndex = CreateScopeManagerIndex();
				list.Add(delegate(IEmitter e)
				{
					e.PushConstant(serviceFactoryIndex, typeof(IServiceFactory));
				});
				list.Add(delegate(IEmitter e)
				{
					e.PushConstant(scopeManagerIndex, typeof(IScopeManager));
				});
				list.Add(delegate(IEmitter e)
				{
					e.PushArgument(1);
				});
				list.Add(delegate(IEmitter e)
				{
					e.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
				});
			}
			if (parameterInfo.ParameterType == typeof(ParameterInfo))
			{
				list.Add(delegate(IEmitter e)
				{
					e.PushConstant(constants.Add(((ConstructorDependency)dependency).Parameter), typeof(ParameterInfo));
				});
			}
			if (parameterInfo.ParameterType == typeof(PropertyInfo))
			{
				list.Add(delegate(IEmitter e)
				{
					e.PushConstant(constants.Add(((PropertyDependency)dependency).Property), typeof(PropertyInfo));
				});
			}
			if (parameterInfo.ParameterType == typeof(object[]))
			{
				list.Add(delegate(IEmitter e)
				{
					PushRuntimeArguments(e);
				});
			}
		}
		int index = constants.Add(dependency.FactoryExpression);
		Type type = dependency.FactoryExpression.GetType();
		MethodInfo declaredMethod = type.GetTypeInfo().GetDeclaredMethod("Invoke");
		emitter.PushConstant(index, type);
		foreach (Action<IEmitter> item in list)
		{
			item(emitter);
		}
		emitter.Call(declaredMethod);
	}

	private void EmitPropertyDependencies(ConstructionInfo constructionInfo, IEmitter emitter)
	{
		if (constructionInfo.PropertyDependencies.Count == 0)
		{
			return;
		}
		LocalBuilder localBuilder = emitter.DeclareLocal(constructionInfo.ImplementingType);
		emitter.Store(localBuilder);
		foreach (PropertyDependency propertyDependency in constructionInfo.PropertyDependencies)
		{
			EmitPropertyDependency(emitter, propertyDependency, localBuilder);
		}
		emitter.Push(localBuilder);
	}

	private Action<IEmitter> CreateEmitMethodForUnknownService(Type serviceType, string serviceName)
	{
		Action<IEmitter> action = null;
		if (CanRedirectRequestForDefaultServiceToSingleNamedService(serviceType, serviceName))
		{
			action = CreateServiceEmitterBasedOnSingleNamedInstance(serviceType);
		}
		else if (serviceType.IsLazy())
		{
			action = CreateEmitMethodBasedOnLazyServiceRequest(serviceType);
		}
		else if (serviceType.IsFuncWithParameters())
		{
			action = CreateEmitMethodBasedParameterizedFuncRequest(serviceType, serviceName);
		}
		else if (serviceType.IsFuncRepresentingService() || serviceType.IsFuncRepresentingNamedService())
		{
			action = CreateEmitMethodBasedOnFuncServiceRequest(serviceType);
		}
		else if (serviceType.IsEnumerableOfT())
		{
			action = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
			if (action == null)
			{
				action = CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);
			}
		}
		else if (serviceType.IsArray)
		{
			action = CreateEmitMethodForArrayServiceRequest(serviceType);
		}
		else if (serviceType.IsReadOnlyCollectionOfT() || serviceType.IsReadOnlyListOfT())
		{
			action = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
			if (action == null)
			{
				action = CreateEmitMethodForReadOnlyCollectionServiceRequest(serviceType);
			}
		}
		else if (serviceType.IsListOfT())
		{
			action = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
			if (action == null)
			{
				action = CreateEmitMethodForListServiceRequest(serviceType);
			}
		}
		else if (serviceType.IsCollectionOfT())
		{
			action = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
			if (action == null)
			{
				action = CreateEmitMethodForListServiceRequest(serviceType);
			}
		}
		else if (serviceType.IsClosedGeneric())
		{
			action = CreateEmitMethodBasedOnClosedGenericServiceRequest(serviceType, serviceName);
		}
		return action;
	}

	private Action<IEmitter> CreateEmitMethodBasedOnFuncServiceRequest(Type serviceType)
	{
		Type type = serviceType.GetTypeInfo().GenericTypeArguments.Last();
		if (serviceType.IsFuncRepresentingService())
		{
			MethodInfo createScopedGenericFuncMethod = FuncHelper.CreateScopedGenericFuncMethod.MakeGenericMethod(type);
			return delegate(IEmitter e)
			{
				e.PushConstant(constants.Add(this), typeof(ServiceContainer));
				int index = CreateScopeManagerIndex();
				e.PushArgument(1);
				e.PushConstant(index, typeof(IScopeManager));
				e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
				e.Emit(OpCodes.Call, createScopedGenericFuncMethod);
			};
		}
		MethodInfo createScopedGenericNamedFuncMethod = FuncHelper.CreateScopedGenericNamedFuncMethod.MakeGenericMethod(type);
		return delegate(IEmitter e)
		{
			e.PushConstant(constants.Add(this), typeof(ServiceContainer));
			int index = CreateScopeManagerIndex();
			e.PushArgument(1);
			e.PushConstant(index, typeof(IScopeManager));
			e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
			e.Emit(OpCodes.Call, createScopedGenericNamedFuncMethod);
		};
	}

	private Action<IEmitter> CreateEmitMethodBasedParameterizedFuncRequest(Type serviceType, string serviceName)
	{
		Delegate value = ((!string.IsNullOrEmpty(serviceName)) ? ReflectionHelper.CreateGetNamedInstanceWithParametersDelegate(this, serviceType, serviceName) : CreateGetInstanceWithParametersDelegate(serviceType));
		int constantIndex = constants.Add(value);
		return delegate(IEmitter e)
		{
			e.PushConstant(constantIndex, serviceType);
		};
	}

	private Delegate CreateGetInstanceWithParametersDelegate(Type serviceType)
	{
		MethodInfo getInstanceWithParametersMethod = ReflectionHelper.GetGetInstanceWithParametersMethod(serviceType);
		return getInstanceWithParametersMethod.CreateDelegate(serviceType, this);
	}

	private Action<IEmitter> CreateServiceEmitterBasedOnFactoryRule(FactoryRule rule, Type serviceType, string serviceName)
	{
		ServiceRegistration serviceRegistration = new ServiceRegistration
		{
			ServiceType = serviceType,
			ServiceName = serviceName,
			FactoryExpression = rule.Factory,
			Lifetime = (CloneLifeTime(rule.LifeTime) ?? DefaultLifetime)
		};
		if (rule.LifeTime != null)
		{
			return delegate(IEmitter emitter)
			{
				EmitLifetime(serviceRegistration, delegate(IEmitter e)
				{
					EmitNewInstanceWithDecorators(serviceRegistration, e);
				}, emitter);
			};
		}
		return delegate(IEmitter emitter)
		{
			EmitNewInstanceWithDecorators(serviceRegistration, emitter);
		};
	}

	private Action<IEmitter> CreateEmitMethodForArrayServiceRequest(Type serviceType)
	{
		return CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);
	}

	private Action<IEmitter> CreateEmitMethodForListServiceRequest(Type serviceType)
	{
		Action<IEmitter> enumerableEmitter = CreateEmitMethodForEnumerableServiceServiceRequest(serviceType);
		MethodInfo declaredMethod = typeof(Enumerable).GetTypeInfo().GetDeclaredMethod("ToList");
		MethodInfo closedGenericToListMethod = declaredMethod.MakeGenericMethod(TypeHelper.GetElementType(serviceType));
		return delegate(IEmitter ms)
		{
			enumerableEmitter(ms);
			ms.Emit(OpCodes.Call, closedGenericToListMethod);
		};
	}

	private Action<IEmitter> CreateEmitMethodForReadOnlyCollectionServiceRequest(Type serviceType)
	{
		Type elementType = TypeHelper.GetElementType(serviceType);
		Type type = typeof(ReadOnlyCollection<>).MakeGenericType(elementType);
		ConstructorInfo constructorInfo = type.GetTypeInfo().DeclaredConstructors.Single();
		Action<IEmitter> listEmitMethod = CreateEmitMethodForListServiceRequest(serviceType);
		return delegate(IEmitter emitter)
		{
			listEmitMethod(emitter);
			emitter.New(constructorInfo);
		};
	}

	private Action<IEmitter> CreateEmitMethodBasedOnLazyServiceRequest(Type serviceType)
	{
		Type type = serviceType.GetTypeInfo().GenericTypeArguments.Last();
		MethodInfo createScopedLazyMethod = LazyHelper.CreateScopedLazyMethod.MakeGenericMethod(type);
		return delegate(IEmitter e)
		{
			e.PushConstant(constants.Add(this), typeof(ServiceContainer));
			int index = CreateScopeManagerIndex();
			e.PushArgument(1);
			e.PushConstant(index, typeof(IScopeManager));
			e.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
			e.Emit(OpCodes.Call, createScopedLazyMethod);
		};
	}

	private ThreadSafeDictionary<string, ServiceRegistration> GetOpenGenericServiceRegistrations(Type openGenericServiceType)
	{
		return GetAvailableServices(openGenericServiceType);
	}

	private Action<IEmitter> CreateEmitMethodBasedOnClosedGenericServiceRequest(Type closedGenericServiceType, string serviceName)
	{
		Type genericTypeDefinition = closedGenericServiceType.GetGenericTypeDefinition();
		ThreadSafeDictionary<string, ServiceRegistration> openGenericServiceRegistrations = GetOpenGenericServiceRegistrations(genericTypeDefinition);
		Dictionary<string, ClosedGenericCandidate> dictionary = new Dictionary<string, ClosedGenericCandidate>(StringComparer.OrdinalIgnoreCase);
		foreach (ServiceRegistration value in openGenericServiceRegistrations.Values)
		{
			Type type = GenericArgumentMapper.TryMakeGenericType(closedGenericServiceType, value.ImplementingType);
			if (type != null && closedGenericServiceType.IsAssignableFrom(type))
			{
				dictionary.Add(value.ServiceName, new ClosedGenericCandidate(type, value.Lifetime));
			}
		}
		ClosedGenericCandidate candidate;
		if (string.IsNullOrWhiteSpace(serviceName))
		{
			string key = string.Empty;
			if (dictionary.Count > 0)
			{
				key = options.DefaultServiceSelector(dictionary.Keys.OrderBy((string k) => k).ToArray());
			}
			if (dictionary.TryGetValue(key, out candidate))
			{
				return RegisterAndGetEmitMethod();
			}
			if (dictionary.Count == 1)
			{
				candidate = dictionary.First().Value;
				return RegisterAndGetEmitMethod();
			}
			return null;
		}
		if (dictionary.TryGetValue(serviceName, out candidate))
		{
			return RegisterAndGetEmitMethod();
		}
		return null;
		Action<IEmitter> RegisterAndGetEmitMethod()
		{
			ServiceRegistration serviceRegistration = new ServiceRegistration
			{
				ServiceType = closedGenericServiceType,
				ImplementingType = candidate.ClosedGenericImplementingType,
				ServiceName = serviceName,
				Lifetime = (CloneLifeTime(candidate.Lifetime) ?? DefaultLifetime)
			};
			Register(serviceRegistration);
			return GetEmitMethod(serviceRegistration.ServiceType, serviceRegistration.ServiceName);
		}
	}

	private Action<IEmitter> CreateEmitMethodForEnumerableServiceServiceRequest(Type serviceType)
	{
		Type actualServiceType = TypeHelper.GetElementType(serviceType);
		if (actualServiceType.GetTypeInfo().IsGenericType)
		{
			Type genericTypeDefinition = actualServiceType.GetGenericTypeDefinition();
			ThreadSafeDictionary<string, ServiceRegistration> openGenericServiceRegistrations = GetOpenGenericServiceRegistrations(genericTypeDefinition);
			var enumerable = from r in openGenericServiceRegistrations.Values
				select new
				{
					Lifetime = r.Lifetime,
					ServiceName = r.ServiceName,
					closedGenericImplementingType = GenericArgumentMapper.TryMakeGenericType(actualServiceType, r.ImplementingType)
				} into t
				where t.closedGenericImplementingType != null
				select t;
			foreach (var item in enumerable)
			{
				ServiceRegistration serviceRegistration = new ServiceRegistration
				{
					ServiceType = actualServiceType,
					ImplementingType = item.closedGenericImplementingType,
					ServiceName = item.ServiceName,
					Lifetime = (CloneLifeTime(item.Lifetime) ?? DefaultLifetime)
				};
				Register(serviceRegistration);
			}
		}
		List<Action<IEmitter>> emitMethods;
		if (options.EnableVariance && options.VarianceFilter(serviceType))
		{
			emitMethods = (from kv in emitters.Where((KeyValuePair<Type, ThreadSafeDictionary<string, Action<IEmitter>>> kv) => actualServiceType.GetTypeInfo().IsAssignableFrom(kv.Key.GetTypeInfo())).SelectMany((KeyValuePair<Type, ThreadSafeDictionary<string, Action<IEmitter>>> kv) => kv.Value)
				orderby kv.Key
				select kv.Value).ToList();
		}
		else
		{
			emitMethods = (from kv in GetEmitMethods(actualServiceType)
				orderby kv.Key
				select kv.Value).ToList();
		}
		if (dependencyStack.Count > 0 && emitMethods.Contains(dependencyStack.Peek()))
		{
			emitMethods.Remove(dependencyStack.Peek());
		}
		return delegate(IEmitter e)
		{
			EmitEnumerable(emitMethods, actualServiceType, e);
		};
	}

	private Action<IEmitter> CreateServiceEmitterBasedOnSingleNamedInstance(Type serviceType)
	{
		return GetEmitMethod(serviceType, GetEmitMethods(serviceType).First().Key);
	}

	private bool CanRedirectRequestForDefaultServiceToSingleNamedService(Type serviceType, string serviceName)
	{
		return string.IsNullOrEmpty(serviceName) && GetEmitMethods(serviceType).Count == 1;
	}

	private ConstructionInfo GetConstructionInfo(Registration registration)
	{
		return constructionInfoProvider.Value.GetConstructionInfo(registration);
	}

	private ThreadSafeDictionary<string, Action<IEmitter>> GetEmitMethods(Type serviceType)
	{
		return emitters.GetOrAdd(serviceType, (Type s) => new ThreadSafeDictionary<string, Action<IEmitter>>(StringComparer.CurrentCultureIgnoreCase));
	}

	private ThreadSafeDictionary<string, ServiceRegistration> GetAvailableServices(Type serviceType)
	{
		return availableServices.GetOrAdd(serviceType, (Type s) => new ThreadSafeDictionary<string, ServiceRegistration>(StringComparer.CurrentCultureIgnoreCase));
	}

	private ThreadSafeDictionary<string, Delegate> GetConstructorDependencyFactories(Type dependencyType)
	{
		return constructorDependencyFactories.GetOrAdd(dependencyType, (Type d) => new ThreadSafeDictionary<string, Delegate>(StringComparer.CurrentCultureIgnoreCase));
	}

	private ThreadSafeDictionary<string, Delegate> GetPropertyDependencyFactories(Type dependencyType)
	{
		return propertyDependencyFactories.GetOrAdd(dependencyType, (Type d) => new ThreadSafeDictionary<string, Delegate>(StringComparer.CurrentCultureIgnoreCase));
	}

	private void RegisterService(Type serviceType, Type implementingType, ILifetime lifetime, string serviceName)
	{
		Ensure.IsNotNull(serviceType, "type");
		Ensure.IsNotNull(implementingType, "implementingType");
		Ensure.IsNotNull(serviceName, "serviceName");
		EnsureConstructable(serviceType, implementingType);
		ServiceRegistration serviceRegistration = new ServiceRegistration
		{
			ServiceType = serviceType,
			ImplementingType = implementingType,
			ServiceName = serviceName,
			Lifetime = (lifetime ?? DefaultLifetime)
		};
		Register(serviceRegistration);
	}

	private void EnsureConstructable(Type serviceType, Type implementingType)
	{
		if (implementingType.GetTypeInfo().ContainsGenericParameters)
		{
			try
			{
				GenericArgumentMapper.Map(serviceType, implementingType).GetMappedArguments();
				return;
			}
			catch (InvalidOperationException ex)
			{
				throw new ArgumentOutOfRangeException("implementingType", ex.Message);
			}
		}
		if (!serviceType.GetTypeInfo().IsAssignableFrom(implementingType.GetTypeInfo()))
		{
			throw new ArgumentOutOfRangeException("implementingType", "The implementing type " + implementingType.FullName + " is not assignable from " + serviceType.FullName + ".");
		}
	}

	private Action<IEmitter> ResolveEmitMethod(ServiceRegistration serviceRegistration)
	{
		return delegate(IEmitter emitter)
		{
			ServiceOverride[] array = overrides.Items.Where((ServiceOverride so) => so.CanOverride(serviceRegistration)).ToArray();
			ServiceOverride[] array2 = array;
			foreach (ServiceOverride serviceOverride in array2)
			{
				serviceRegistration = serviceOverride.Execute(this, serviceRegistration);
			}
			if (serviceRegistration.Lifetime == null)
			{
				if (options.OptimizeForLargeObjectGraphs)
				{
					EmitLifetime(serviceRegistration, delegate(IEmitter e)
					{
						EmitNewInstanceWithDecorators(serviceRegistration, e);
					}, emitter);
				}
				else
				{
					EmitNewInstanceWithDecorators(serviceRegistration, emitter);
				}
			}
			else
			{
				EmitLifetime(serviceRegistration, delegate(IEmitter e)
				{
					EmitNewInstanceWithDecorators(serviceRegistration, e);
				}, emitter);
			}
		};
	}

	private void EmitNewInstanceWithDecorators(ServiceRegistration serviceRegistration, IEmitter emitter)
	{
		DecoratorRegistration[] array = GetDecorators(serviceRegistration);
		if (array.Length != 0)
		{
			EmitDecorators(serviceRegistration, array, emitter, delegate(IEmitter dm)
			{
				EmitNewInstance(serviceRegistration, dm);
			});
		}
		else
		{
			EmitNewInstance(serviceRegistration, emitter);
		}
		if (serviceRegistration.Lifetime is PerContainerLifetime && IsNotServiceFactory(serviceRegistration.ServiceType))
		{
			MethodInfo methodInfo = OpenGenericTrackInstanceMethod.MakeGenericMethod(emitter.StackType);
			int index = constants.Add(this);
			emitter.PushConstant(index, typeof(ServiceContainer));
			emitter.Emit(OpCodes.Call, methodInfo);
		}
		Initializer[] array2 = initializers.Items.Where((Initializer i) => i.Predicate(serviceRegistration)).ToArray();
		if (array2.Length != 0)
		{
			LocalBuilder localBuilder = emitter.DeclareLocal(serviceRegistration.ServiceType);
			emitter.Store(localBuilder);
			Initializer[] array3 = array2;
			foreach (Initializer initializer in array3)
			{
				Type type = initializer.Initialize.GetType();
				int index2 = constants.Add(initializer.Initialize);
				emitter.PushConstant(index2, type);
				int index3 = constants.Add(this);
				emitter.PushConstant(index3, typeof(IServiceFactory));
				int index4 = CreateScopeManagerIndex();
				emitter.PushConstant(index4, typeof(IScopeManager));
				emitter.PushArgument(1);
				emitter.Emit(OpCodes.Call, ServiceFactoryLoader.LoadServiceFactoryMethod);
				emitter.Push(localBuilder);
				MethodInfo declaredMethod = type.GetTypeInfo().GetDeclaredMethod("Invoke");
				emitter.Call(declaredMethod);
			}
			emitter.Push(localBuilder);
		}
		static bool IsNotServiceFactory(Type serviceType)
		{
			return !typeof(IServiceFactory).GetTypeInfo().IsAssignableFrom(serviceType.GetTypeInfo());
		}
	}

	private int GetInstanceDelegateIndex(ServiceRegistration serviceRegistration, Action<IEmitter> emitMethod)
	{
		if (servicesToDelegatesIndex.ContainsUninitializedValue(serviceRegistration))
		{
			throw new InvalidOperationException($"Recursive dependency detected: ServiceType:{serviceRegistration.ServiceType}, ServiceName:{serviceRegistration.ServiceName}]");
		}
		return servicesToDelegatesIndex.GetOrAdd(serviceRegistration, (ServiceRegistration _) => CreateInstanceDelegateIndex(emitMethod));
	}

	private void EmitLifetime(ServiceRegistration serviceRegistration, Action<IEmitter> emitMethod, IEmitter emitter)
	{
		if (serviceRegistration.Lifetime == null)
		{
			int instanceDelegateIndex = GetInstanceDelegateIndex(serviceRegistration, emitMethod);
			MethodInfo declaredMethod = typeof(GetInstanceDelegate).GetTypeInfo().GetDeclaredMethod("Invoke");
			emitter.PushConstant(instanceDelegateIndex, typeof(GetInstanceDelegate));
			emitter.PushArgument(0);
			PushScope(emitter);
			emitter.Emit(OpCodes.Callvirt, declaredMethod);
			return;
		}
		if (serviceRegistration.Lifetime is PerScopeLifetime)
		{
			int orAdd = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, (ServiceRegistration _) => CreateInstanceDelegateIndex(emitMethod));
			PushScope(emitter);
			emitter.Emit(OpCodes.Call, ScopeLoader.ValidateScopeMethod.MakeGenericMethod(serviceRegistration.ServiceType.UnderlyingSystemType));
			emitter.PushConstant(orAdd, typeof(GetInstanceDelegate));
			emitter.PushArgument(0);
			emitter.Push(orAdd);
			emitter.Call(ScopeLoader.GetScopedInstanceMethod);
		}
		else if (serviceRegistration.Lifetime is PerRequestLifeTime)
		{
			LocalBuilder localBuilder = emitter.DeclareLocal(typeof(Scope));
			int orAdd2 = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, (ServiceRegistration _) => CreateInstanceDelegateIndex(emitMethod));
			MethodInfo declaredMethod2 = typeof(GetInstanceDelegate).GetTypeInfo().GetDeclaredMethod("Invoke");
			emitter.PushConstant(orAdd2, typeof(GetInstanceDelegate));
			emitter.PushArgument(0);
			PushScope(emitter);
			emitter.Store(localBuilder);
			emitter.Push(localBuilder);
			emitter.Emit(OpCodes.Callvirt, declaredMethod2);
			emitter.Push(localBuilder);
			emitter.Emit(OpCodes.Call, ScopeLoader.ValidateTrackedTransientMethod);
		}
		else
		{
			MethodInfo nonClosingGetInstanceMethod = LifetimeHelper.GetNonClosingGetInstanceMethod(serviceRegistration.Lifetime.GetType());
			if (nonClosingGetInstanceMethod != null)
			{
				int orAdd3 = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, (ServiceRegistration _) => CreateInstanceDelegateIndex(emitMethod));
				int index = CreateLifetimeIndex(serviceRegistration.Lifetime);
				emitter.PushConstant(index, serviceRegistration.Lifetime.GetType());
				emitter.PushConstant(orAdd3, typeof(GetInstanceDelegate));
				PushScope(emitter);
				emitter.PushArgument(0);
				emitter.Call(nonClosingGetInstanceMethod);
			}
			else
			{
				int orAdd4 = servicesToDelegatesIndex.GetOrAdd(serviceRegistration, (ServiceRegistration _) => CreateInstanceDelegateIndex(emitMethod));
				int index2 = CreateLifetimeIndex(serviceRegistration.Lifetime);
				LocalBuilder localBuilder2 = emitter.DeclareLocal(typeof(Scope));
				PushScope(emitter);
				emitter.Store(localBuilder2);
				emitter.PushConstant(index2, typeof(ILifetime));
				emitter.PushConstant(orAdd4, typeof(GetInstanceDelegate));
				emitter.PushArgument(0);
				emitter.Push(localBuilder2);
				emitter.Emit(OpCodes.Call, FuncHelper.CreateScopedFuncMethod);
				emitter.Push(localBuilder2);
				emitter.Call(LifetimeHelper.GetInstanceMethod);
			}
		}
		if (IsNotServiceFactory(serviceRegistration.ServiceType) && !(serviceRegistration.Lifetime is PerContainerLifetime))
		{
			disposableLifeTimes.Add(serviceRegistration.Lifetime);
		}
		static bool IsNotServiceFactory(Type serviceType)
		{
			return !typeof(IServiceFactory).GetTypeInfo().IsAssignableFrom(serviceType.GetTypeInfo());
		}
	}

	private void PushScope(IEmitter emitter)
	{
		if (options.EnableCurrentScope)
		{
			int index = CreateScopeManagerIndex();
			emitter.PushArgument(1);
			emitter.PushConstant(index, typeof(IScopeManager));
			emitter.Emit(OpCodes.Call, ScopeLoader.GetThisOrCurrentScopeMethod);
		}
		else
		{
			emitter.PushArgument(1);
		}
	}

	private int CreateScopeManagerIndex()
	{
		return constants.Add(ScopeManagerProvider.GetScopeManager(this));
	}

	private int CreateInstanceDelegateIndex(Action<IEmitter> emitMethod)
	{
		return constants.Add(CreateDynamicMethodDelegate(emitMethod));
	}

	private int CreateLifetimeIndex(ILifetime lifetime)
	{
		return constants.Add(lifetime);
	}

	private GetInstanceDelegate CreateDefaultDelegate(Type serviceType, bool throwError)
	{
		log.Info($"Compiling delegate for resolving service : {serviceType}");
		GetInstanceDelegate getInstanceDelegate = CreateDelegate(serviceType, string.Empty, throwError);
		if (getInstanceDelegate == null)
		{
			return (object[] args, Scope scope) => (object)null;
		}
		Interlocked.Exchange(ref delegates, delegates.Add(serviceType, getInstanceDelegate));
		return getInstanceDelegate;
	}

	private GetInstanceDelegate CreateNamedDelegate((Type, string) key, bool throwError)
	{
		log.Info($"Compiling delegate for resolving service : {key.Item1}, name: {key.Item2}");
		GetInstanceDelegate getInstanceDelegate = CreateDelegate(key.Item1, key.Item2, throwError);
		if (getInstanceDelegate == null)
		{
			return (object[] args, Scope scope) => (object)null;
		}
		Interlocked.Exchange(ref namedDelegates, namedDelegates.Add(key, getInstanceDelegate));
		return getInstanceDelegate;
	}

	private GetInstanceDelegate CreateDelegate(Type serviceType, string serviceName, bool throwError)
	{
		lock (lockObject)
		{
			Action<IEmitter> emitMethod = GetEmitMethod(serviceType, serviceName);
			if ((emitMethod == null) & throwError)
			{
				servicesToDelegatesIndex.Remove(new ServiceRegistration
				{
					ServiceType = serviceType,
					ServiceName = serviceName
				});
				throw new InvalidOperationException($"Unable to resolve type: {serviceType}, service name: {serviceName}");
			}
			if (emitMethod != null)
			{
				try
				{
					return CreateDynamicMethodDelegate(emitMethod);
				}
				catch (InvalidOperationException innerException)
				{
					dependencyStack.Clear();
					servicesToDelegatesIndex.Remove(new ServiceRegistration
					{
						ServiceType = serviceType,
						ServiceName = serviceName
					});
					throw new InvalidOperationException($"Unable to resolve type: {serviceType}, service name: {serviceName}", innerException);
				}
			}
			return null;
		}
	}

	private void RegisterValue(Type serviceType, object value, string serviceName)
	{
		ServiceRegistration serviceRegistration = new ServiceRegistration
		{
			ServiceType = serviceType,
			ServiceName = serviceName,
			Value = value,
			Lifetime = new PerContainerLifetime()
		};
		Register(serviceRegistration);
	}

	private void RegisterServiceFromLambdaExpression<TService>(Delegate factory, ILifetime lifetime, string serviceName)
	{
		ServiceRegistration serviceRegistration = new ServiceRegistration();
		serviceRegistration.ServiceType = typeof(TService);
		serviceRegistration.FactoryExpression = factory;
		serviceRegistration.ServiceName = serviceName;
		serviceRegistration.Lifetime = lifetime ?? DefaultLifetime;
		ServiceRegistration serviceRegistration2 = serviceRegistration;
		Register(serviceRegistration2);
	}
}
