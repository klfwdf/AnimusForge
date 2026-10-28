using System.Collections.Generic;
using System.Reflection;

namespace MCM.LightInject;

internal interface IConstructorDependencySelector
{
	IEnumerable<ConstructorDependency> Execute(ConstructorInfo constructor);
}
