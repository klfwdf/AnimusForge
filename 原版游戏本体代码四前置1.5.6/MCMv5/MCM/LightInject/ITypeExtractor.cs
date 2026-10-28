using System;
using System.Reflection;

namespace MCM.LightInject;

internal interface ITypeExtractor
{
	Type[] Execute(Assembly assembly);
}
