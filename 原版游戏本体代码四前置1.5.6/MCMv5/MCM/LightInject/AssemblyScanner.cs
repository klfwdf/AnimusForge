using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class AssemblyScanner : IAssemblyScanner
{
	private readonly ITypeExtractor concreteTypeExtractor;

	private readonly ITypeExtractor compositionRootTypeExtractor;

	private readonly ICompositionRootExecutor compositionRootExecutor;

	private readonly IGenericArgumentMapper genericArgumentMapper;

	private Assembly currentAssembly;

	public AssemblyScanner(ITypeExtractor concreteTypeExtractor, ITypeExtractor compositionRootTypeExtractor, ICompositionRootExecutor compositionRootExecutor, IGenericArgumentMapper genericArgumentMapper)
	{
		this.concreteTypeExtractor = concreteTypeExtractor;
		this.compositionRootTypeExtractor = compositionRootTypeExtractor;
		this.compositionRootExecutor = compositionRootExecutor;
		this.genericArgumentMapper = genericArgumentMapper;
	}

	public void Scan(Assembly assembly, IServiceRegistry serviceRegistry, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
	{
		Type[] concreteTypes = GetConcreteTypes(assembly);
		Type[] array = concreteTypes;
		foreach (Type implementingType in array)
		{
			BuildImplementationMap(implementingType, serviceRegistry, lifetimeFactory, shouldRegister, serviceNameProvider);
		}
	}

	public void Scan(Assembly assembly, IServiceRegistry serviceRegistry)
	{
		Type[] compositionRootTypes = GetCompositionRootTypes(assembly);
		if (compositionRootTypes.Length != 0 && !object.Equals(currentAssembly, assembly))
		{
			currentAssembly = assembly;
			ExecuteCompositionRoots(compositionRootTypes);
		}
	}

	private static IEnumerable<Type> GetBaseTypes(Type concreteType)
	{
		Type baseType = concreteType;
		while (baseType != typeof(object) && baseType != null)
		{
			yield return baseType;
			baseType = baseType.GetTypeInfo().BaseType;
		}
	}

	private void ExecuteCompositionRoots(IEnumerable<Type> compositionRoots)
	{
		foreach (Type compositionRoot in compositionRoots)
		{
			compositionRootExecutor.Execute(compositionRoot);
		}
	}

	private Type[] GetConcreteTypes(Assembly assembly)
	{
		return concreteTypeExtractor.Execute(assembly);
	}

	private Type[] GetCompositionRootTypes(Assembly assembly)
	{
		return compositionRootTypeExtractor.Execute(assembly);
	}

	private void BuildImplementationMap(Type implementingType, IServiceRegistry serviceRegistry, Func<ILifetime> lifetimeFactory, Func<Type, Type, bool> shouldRegister, Func<Type, Type, string> serviceNameProvider)
	{
		Type[] array = implementingType.GetTypeInfo().ImplementedInterfaces.ToArray();
		Type[] array2 = array;
		foreach (Type type in array2)
		{
			if (shouldRegister(type, implementingType))
			{
				RegisterInternal(type, implementingType, serviceRegistry, lifetimeFactory(), serviceNameProvider);
			}
		}
		foreach (Type baseType in GetBaseTypes(implementingType))
		{
			if (shouldRegister(baseType, implementingType))
			{
				RegisterInternal(baseType, implementingType, serviceRegistry, lifetimeFactory(), serviceNameProvider);
			}
		}
	}

	private void RegisterInternal(Type serviceType, Type implementingType, IServiceRegistry serviceRegistry, ILifetime lifetime, Func<Type, Type, string> serviceNameProvider)
	{
		TypeInfo typeInfo = serviceType.GetTypeInfo();
		if (!implementingType.GetTypeInfo().ContainsGenericParameters || genericArgumentMapper.Map(serviceType, implementingType).IsValid)
		{
			if (typeInfo.IsGenericType && typeInfo.ContainsGenericParameters)
			{
				serviceType = typeInfo.GetGenericTypeDefinition();
			}
			serviceRegistry.Register(serviceType, implementingType, serviceNameProvider(serviceType, implementingType), lifetime);
		}
	}
}
