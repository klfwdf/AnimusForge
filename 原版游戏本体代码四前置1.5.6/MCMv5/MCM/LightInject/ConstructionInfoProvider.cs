using System.Diagnostics.CodeAnalysis;

namespace MCM.LightInject;

[ExcludeFromCodeCoverage]
internal class ConstructionInfoProvider : IConstructionInfoProvider
{
	private readonly IConstructionInfoBuilder constructionInfoBuilder;

	private readonly ThreadSafeDictionary<Registration, ConstructionInfo> cache = new ThreadSafeDictionary<Registration, ConstructionInfo>();

	public ConstructionInfoProvider(IConstructionInfoBuilder constructionInfoBuilder)
	{
		this.constructionInfoBuilder = constructionInfoBuilder;
	}

	public ConstructionInfo GetConstructionInfo(Registration registration)
	{
		return cache.GetOrAdd(registration, constructionInfoBuilder.Execute);
	}
}
