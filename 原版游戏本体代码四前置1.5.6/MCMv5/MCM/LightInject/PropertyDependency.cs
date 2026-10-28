using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class PropertyDependency : Dependency
{
	public PropertyInfo Property { get; set; }

	public override string Name => Property.Name;

	public override string ToString()
	{
		return $"[Target Type: {Property.DeclaringType}], [Property: {Property.Name}({Property.PropertyType})]" + ", " + base.ToString();
	}
}
