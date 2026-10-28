using System;
using System.Collections.Generic;

namespace MCM.LightInject;

internal interface IPropertyDependencySelector
{
	IEnumerable<PropertyDependency> Execute(Type type);
}
