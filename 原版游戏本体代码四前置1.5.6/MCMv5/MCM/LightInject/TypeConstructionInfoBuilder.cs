using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class TypeConstructionInfoBuilder : IConstructionInfoBuilder
{
	private readonly IConstructorSelector constructorSelector;

	private readonly IConstructorDependencySelector constructorDependencySelector;

	private readonly IPropertyDependencySelector propertyDependencySelector;

	private readonly Func<Type, string, Delegate> getConstructorDependencyExpression;

	private readonly Func<Type, string, Delegate> getPropertyDependencyExpression;

	public TypeConstructionInfoBuilder(IConstructorSelector constructorSelector, IConstructorDependencySelector constructorDependencySelector, IPropertyDependencySelector propertyDependencySelector, Func<Type, string, Delegate> getConstructorDependencyExpression, Func<Type, string, Delegate> getPropertyDependencyExpression)
	{
		this.constructorSelector = constructorSelector;
		this.constructorDependencySelector = constructorDependencySelector;
		this.propertyDependencySelector = propertyDependencySelector;
		this.getConstructorDependencyExpression = getConstructorDependencyExpression;
		this.getPropertyDependencyExpression = getPropertyDependencyExpression;
	}

	public ConstructionInfo Execute(Registration registration)
	{
		if ((object)registration.FactoryExpression != null)
		{
			return new ConstructionInfo
			{
				FactoryDelegate = registration.FactoryExpression
			};
		}
		Type implementingType = registration.ImplementingType;
		ConstructionInfo constructionInfo = new ConstructionInfo
		{
			ImplementingType = implementingType
		};
		constructionInfo.PropertyDependencies.AddRange(GetPropertyDependencies(implementingType));
		constructionInfo.Constructor = constructorSelector.Execute(implementingType);
		constructionInfo.ConstructorDependencies.AddRange(GetConstructorDependencies(constructionInfo.Constructor));
		return constructionInfo;
	}

	private IEnumerable<ConstructorDependency> GetConstructorDependencies(ConstructorInfo constructorInfo)
	{
		ConstructorDependency[] array = constructorDependencySelector.Execute(constructorInfo).ToArray();
		ConstructorDependency[] array2 = array;
		foreach (ConstructorDependency constructorDependency in array2)
		{
			constructorDependency.FactoryExpression = getConstructorDependencyExpression(constructorDependency.ServiceType, constructorDependency.ServiceName);
		}
		return array;
	}

	private IEnumerable<PropertyDependency> GetPropertyDependencies(Type implementingType)
	{
		PropertyDependency[] array = propertyDependencySelector.Execute(implementingType).ToArray();
		PropertyDependency[] array2 = array;
		foreach (PropertyDependency propertyDependency in array2)
		{
			propertyDependency.FactoryExpression = getPropertyDependencyExpression(propertyDependency.ServiceType, propertyDependency.ServiceName);
		}
		return array;
	}
}
