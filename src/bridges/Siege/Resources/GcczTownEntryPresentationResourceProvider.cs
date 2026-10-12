using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

/// <summary>
/// Loads the localized staged town entry presentation once per process.
/// </summary>
internal static class GcczTownEntryPresentationResourceProvider
{
	private const string BaseName = "GcczTownEntryPresentation";
	private static readonly Lazy<TownEntryPresentationTextCatalog> ChineseCatalog = new Lazy<TownEntryPresentationTextCatalog>(() => LoadCatalog("zh-CN"), true);
	private static readonly Lazy<TownEntryPresentationTextCatalog> EnglishCatalog = new Lazy<TownEntryPresentationTextCatalog>(() => LoadCatalog("en"), true);

	internal static TownEntryPresentationTextCatalog GetCatalog()
	{
		return GcczLocalizedResourceLoader.UseChineseInterface ? ChineseCatalog.Value : EnglishCatalog.Value;
	}

	internal static TownEntryPresentationTextCatalog GetFactCatalog() => ChineseCatalog.Value;

	private static TownEntryPresentationTextCatalog LoadCatalog(string language)
	{
		return GcczLocalizedResourceLoader.Load(
			BaseName + "." + language + ".json",
			"AnimusForge.Defaults." + BaseName + "." + language + ".json",
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
