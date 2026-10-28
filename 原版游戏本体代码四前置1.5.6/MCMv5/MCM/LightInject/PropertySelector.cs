using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PropertySelector : IPropertySelector
{
	public IEnumerable<PropertyInfo> Execute(Type type)
	{
		return type.GetRuntimeProperties().Where(IsInjectable).ToList();
	}

	protected virtual bool IsInjectable(PropertyInfo propertyInfo)
	{
		return !IsReadOnly(propertyInfo);
	}

	private static bool IsReadOnly(PropertyInfo propertyInfo)
	{
		return propertyInfo.SetMethod == null || propertyInfo.SetMethod.IsStatic || propertyInfo.SetMethod.IsPrivate || propertyInfo.GetIndexParameters().Length != 0;
	}
}
