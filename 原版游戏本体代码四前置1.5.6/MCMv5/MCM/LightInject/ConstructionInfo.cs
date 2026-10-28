using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ConstructionInfo
{
	public Type ImplementingType { get; set; }

	public ConstructorInfo Constructor { get; set; }

	public List<PropertyDependency> PropertyDependencies { get; private set; }

	public List<ConstructorDependency> ConstructorDependencies { get; private set; }

	public Delegate FactoryDelegate { get; set; }

	public ConstructionInfo()
	{
		PropertyDependencies = new List<PropertyDependency>();
		ConstructorDependencies = new List<ConstructorDependency>();
	}
}
