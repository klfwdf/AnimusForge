using System;
using AnimusForge.SiegeAftermathIntervention;
using Newtonsoft.Json;

namespace AnimusForge;

internal static class GcczTownActionPresentationResourceProvider
{
	private const string BaseName = "GcczTownActionPresentation";
	private static readonly Lazy<TownActionPresentationTextCatalog> ChineseCatalog = new Lazy<TownActionPresentationTextCatalog>(() => LoadCatalog("zh-CN"), true);
	private static readonly Lazy<TownActionPresentationTextCatalog> EnglishCatalog = new Lazy<TownActionPresentationTextCatalog>(() => LoadCatalog("en"), true);

	internal static TownActionPresentationTextCatalog GetCatalog()
	{
		return GcczLocalizedResourceLoader.UseChineseInterface ? ChineseCatalog.Value : EnglishCatalog.Value;
	}

	internal static TownActionPresentationTextCatalog GetFactCatalog() => ChineseCatalog.Value;

	private static TownActionPresentationTextCatalog LoadCatalog(string language)
	{
		return GcczLocalizedResourceLoader.Load(
			BaseName + "." + language + ".json",
			"AnimusForge.Defaults." + BaseName + "." + language + ".json",
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
