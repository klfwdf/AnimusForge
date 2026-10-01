using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class GcczTownActionPresentationResourceProvider
{
	private const string FileName = "GcczTownActionPresentation.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownActionPresentation.zh-CN.json";
	private static readonly Lazy<TownActionPresentationTextCatalog> CachedCatalog = new Lazy<TownActionPresentationTextCatalog>(LoadCatalog, true);

	internal static TownActionPresentationTextCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownActionPresentationTextCatalog LoadCatalog()
	{
		return GcczLocalizedResourceLoader.Load(
			FileName,
			EmbeddedResourceName,
			"GcczActionPresentation",
			Deserialize,
			TownActionPresentationTextCatalog.CreateEnglishFallback);
	}

	private static TownActionPresentationTextCatalog Deserialize(string json)
	{
		TownActionPresentationTextCatalog parsed = JsonConvert.DeserializeObject<TownActionPresentationTextCatalog>(json ?? string.Empty);
		return TownActionPresentationTextCatalog.Resolve(parsed);
	}
}
