using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized staged town entry presentation once per process.
/// </summary>
internal static class GcczTownEntryPresentationResourceProvider
{
	private const string FileName = "GcczTownEntryPresentation.zh-CN.json";
	private const string EmbeddedResourceName = "AnimusForge.Defaults.GcczTownEntryPresentation.zh-CN.json";
	private static readonly Lazy<TownEntryPresentationTextCatalog> CachedCatalog = new Lazy<TownEntryPresentationTextCatalog>(LoadCatalog, true);

	internal static TownEntryPresentationTextCatalog GetCatalog()
	{
		return CachedCatalog.Value;
	}

	private static TownEntryPresentationTextCatalog LoadCatalog()
	{
		return GcczLocalizedResourceLoader.Load(
			FileName,
			EmbeddedResourceName,
			"GcczEntry",
			Deserialize,
			TownEntryPresentationTextCatalog.CreateEnglishFallback);
	}

	private static TownEntryPresentationTextCatalog Deserialize(string json)
	{
		TownEntryPresentationTextCatalog parsed = JsonConvert.DeserializeObject<TownEntryPresentationTextCatalog>(json ?? string.Empty);
		return TownEntryPresentationTextCatalog.Resolve(parsed);
	}
}
