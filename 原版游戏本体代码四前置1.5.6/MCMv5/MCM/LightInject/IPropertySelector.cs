using System;
using System.Collections.Generic;
using System.Reflection;

namespace MCM.LightInject;

internal interface IPropertySelector
{
	IEnumerable<PropertyInfo> Execute(Type type);
}
