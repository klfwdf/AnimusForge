using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class CachedTypeExtractor : ITypeExtractor
{
	private readonly ITypeExtractor typeExtractor;

	private readonly ThreadSafeDictionary<Assembly, Type[]> cache = new ThreadSafeDictionary<Assembly, Type[]>();

	public CachedTypeExtractor(ITypeExtractor typeExtractor)
	{
		this.typeExtractor = typeExtractor;
	}

	public Type[] Execute(Assembly assembly)
	{
		return cache.GetOrAdd(assembly, typeExtractor.Execute);
	}
}
