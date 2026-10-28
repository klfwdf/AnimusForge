using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ConstructorDependencySelector : IConstructorDependencySelector
{
	public virtual IEnumerable<ConstructorDependency> Execute(ConstructorInfo constructor)
	{
		return from p in constructor.GetParameters()
			orderby p.Position
			select new ConstructorDependency
			{
				ServiceName = string.Empty,
				ServiceType = p.ParameterType,
				Parameter = p,
				IsRequired = true
			};
	}
}
