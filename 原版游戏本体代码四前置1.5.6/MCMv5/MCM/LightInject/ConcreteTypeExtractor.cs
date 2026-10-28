using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ConcreteTypeExtractor : ITypeExtractor
{
	private static readonly List<Type> InternalTypes;

	static ConcreteTypeExtractor()
	{
		InternalTypes = new List<Type>();
		InternalTypes.Add(typeof(ConstructorDependency));
		InternalTypes.Add(typeof(PropertyDependency));
		InternalTypes.Add(typeof(ThreadSafeDictionary<, >));
		InternalTypes.Add(typeof(Scope));
		InternalTypes.Add(typeof(PerContainerLifetime));
		InternalTypes.Add(typeof(PerScopeLifetime));
		InternalTypes.Add(typeof(ServiceRegistration));
		InternalTypes.Add(typeof(DecoratorRegistration));
		InternalTypes.Add(typeof(ServiceRequest));
		InternalTypes.Add(typeof(Registration));
		InternalTypes.Add(typeof(ServiceContainer));
		InternalTypes.Add(typeof(ConstructionInfo));
		InternalTypes.Add(typeof(AssemblyLoader));
		InternalTypes.Add(typeof(TypeConstructionInfoBuilder));
		InternalTypes.Add(typeof(ConstructionInfoProvider));
		InternalTypes.Add(typeof(MostResolvableConstructorSelector));
		InternalTypes.Add(typeof(PerRequestLifeTime));
		InternalTypes.Add(typeof(PropertySelector));
		InternalTypes.Add(typeof(AssemblyScanner));
		InternalTypes.Add(typeof(ConstructorDependencySelector));
		InternalTypes.Add(typeof(PropertyDependencySelector));
		InternalTypes.Add(typeof(CompositionRootTypeAttribute));
		InternalTypes.Add(typeof(ConcreteTypeExtractor));
		InternalTypes.Add(typeof(CompositionRootExecutor));
		InternalTypes.Add(typeof(CompositionRootTypeExtractor));
		InternalTypes.Add(typeof(CachedTypeExtractor));
		InternalTypes.Add(typeof(ImmutableList<>));
		InternalTypes.Add(typeof(KeyValue<, >));
		InternalTypes.Add(typeof(ImmutableHashTree<, >));
		InternalTypes.Add(typeof(ImmutableHashTable<, >));
		InternalTypes.Add(typeof(PerThreadScopeManagerProvider));
		InternalTypes.Add(typeof(Emitter));
		InternalTypes.Add(typeof(Instruction));
		InternalTypes.Add(typeof(Instruction<>));
		InternalTypes.Add(typeof(GetInstanceDelegate));
		InternalTypes.Add(typeof(ContainerOptions));
		InternalTypes.Add(typeof(CompositionRootAttributeExtractor));
		InternalTypes.Add(typeof(PerLogicalCallContextScopeManagerProvider));
		InternalTypes.Add(typeof(PerLogicalCallContextScopeManager));
		InternalTypes.Add(typeof(LogicalThreadStorage<>));
		InternalTypes.Add(typeof(DynamicMethod));
		InternalTypes.Add(typeof(ILGenerator));
		InternalTypes.Add(typeof(LocalBuilder));
	}

	public Type[] Execute(Assembly assembly)
	{
		return assembly.DefinedTypes.Where((TypeInfo info) => IsConcreteType(info)).Except(InternalTypes.Select((Type i) => i.GetTypeInfo())).Cast<Type>()
			.ToArray();
	}

	private static bool IsConcreteType(TypeInfo typeInfo)
	{
		return typeInfo.IsClass && !typeInfo.IsNestedPrivate && !typeInfo.IsAbstract && !object.Equals(typeInfo.Assembly, typeof(string).GetTypeInfo().Assembly) && !IsCompilerGenerated(typeInfo);
	}

	private static bool IsCompilerGenerated(TypeInfo typeInfo)
	{
		return typeInfo.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
	}
}
