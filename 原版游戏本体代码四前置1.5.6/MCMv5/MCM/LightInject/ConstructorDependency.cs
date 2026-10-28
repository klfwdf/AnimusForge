using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ConstructorDependency : Dependency
{
	public ParameterInfo Parameter { get; set; }

	public bool IsDecoratorTarget { get; set; }

	public override string Name => Parameter.Name;

	public override string ToString()
	{
		return $"[Target Type: {Parameter.Member.DeclaringType}], [Parameter: {Parameter.Name}({Parameter.ParameterType})]" + ", " + base.ToString();
	}
}
