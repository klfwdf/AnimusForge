using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class CompositionRootTypeExtractor : ITypeExtractor
{
	private readonly ICompositionRootAttributeExtractor compositionRootAttributeExtractor;

	public CompositionRootTypeExtractor(ICompositionRootAttributeExtractor compositionRootAttributeExtractor)
	{
		this.compositionRootAttributeExtractor = compositionRootAttributeExtractor;
	}

	public Type[] Execute(Assembly assembly)
	{
		CompositionRootTypeAttribute[] attributes = compositionRootAttributeExtractor.GetAttributes(assembly);
		if (attributes.Length != 0)
		{
			return attributes.Select((CompositionRootTypeAttribute a) => a.CompositionRootType).ToArray();
		}
		return assembly.DefinedTypes.Where((TypeInfo t) => !t.IsAbstract && typeof(ICompositionRoot).GetTypeInfo().IsAssignableFrom(t)).Cast<Type>().ToArray();
	}
}
