using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PropertyDependencySelector : IPropertyDependencySelector
{
	protected IPropertySelector PropertySelector { get; private set; }

	public PropertyDependencySelector(IPropertySelector propertySelector)
	{
		PropertySelector = propertySelector;
	}

	public virtual IEnumerable<PropertyDependency> Execute(Type type)
	{
		return from p in PropertySelector.Execute(type)
			select new PropertyDependency
			{
				Property = p,
				ServiceName = string.Empty,
				ServiceType = p.PropertyType
			};
	}
}
