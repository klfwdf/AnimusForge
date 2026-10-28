using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class CompositionRootAttributeExtractor : ICompositionRootAttributeExtractor
{
	public CompositionRootTypeAttribute[] GetAttributes(Assembly assembly)
	{
		return assembly.GetCustomAttributes(typeof(CompositionRootTypeAttribute)).Cast<CompositionRootTypeAttribute>().ToArray();
	}
}
