using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class MostResolvableConstructorSelector : IConstructorSelector
{
	private readonly Func<Type, string, bool> canGetInstance;

	private readonly bool enableOptionalArguments;

	public MostResolvableConstructorSelector(Func<Type, string, bool> canGetInstance, bool enableOptionalArguments)
	{
		this.canGetInstance = canGetInstance;
		this.enableOptionalArguments = enableOptionalArguments;
	}

	public MostResolvableConstructorSelector(Func<Type, string, bool> canGetInstance)
		: this(canGetInstance, enableOptionalArguments: false)
	{
	}

	public ConstructorInfo Execute(Type implementingType)
	{
		ConstructorInfo[] array = implementingType.GetTypeInfo().DeclaredConstructors.Where((ConstructorInfo c) => c.IsPublic && !c.IsStatic).ToArray();
		if (array.Length == 0)
		{
			throw new InvalidOperationException("Missing public constructor for Type: " + implementingType.FullName);
		}
		if (array.Length == 1)
		{
			return array[0];
		}
		foreach (ConstructorInfo item in array.OrderByDescending((ConstructorInfo c) => c.GetParameters().Length))
		{
			ParameterInfo[] parameters = item.GetParameters();
			if (CanCreateParameterDependencies(parameters))
			{
				return item;
			}
		}
		throw new InvalidOperationException("No resolvable constructor found for Type: " + implementingType.FullName);
	}

	protected virtual string GetServiceName(ParameterInfo parameter)
	{
		return parameter.Name;
	}

	private bool CanCreateParameterDependencies(IEnumerable<ParameterInfo> parameters)
	{
		return parameters.All(CanCreateParameterDependency);
	}

	private bool CanCreateParameterDependency(ParameterInfo parameterInfo)
	{
		return canGetInstance(parameterInfo.ParameterType, string.Empty) || canGetInstance(parameterInfo.ParameterType, GetServiceName(parameterInfo)) || (parameterInfo.HasDefaultValue && enableOptionalArguments);
	}
}
