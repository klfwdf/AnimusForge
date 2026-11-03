using System;

using System.IO;

using System.Text;

using AnimusForge.SiegeAftermathIntervention;

using Newtonsoft.Json;



namespace AnimusForge;



/// <summary>

/// Loads the localized GCCZ town prompt text once per process.

/// The reusable composer and all prompt policy remain in the GCCZ core project.

/// </summary>

internal static class GcczTownPromptResourceProvider

{

	private static readonly Lazy<TownPromptTextCatalog> ChinesePresentation = new Lazy<TownPromptTextCatalog>(() => LoadPresentation("zh-CN"), true);

	private static readonly Lazy<TownPromptTextCatalog> EnglishPresentation = new Lazy<TownPromptTextCatalog>(() => LoadPresentation("en"), true);

	// Fixed GCCZ prompt text follows the game's text language; both editions ship with every package.
	internal static TownPromptTextCatalog GetCatalog()
		=> PromptLanguage.IsChinese ? ChinesePresentation.Value : EnglishPresentation.Value;

	internal static TownPromptTextCatalog GetPresentationCatalog()
		=> GcczLocalizedResourceLoader.UseChineseInterface ? ChinesePresentation.Value : EnglishPresentation.Value;

	internal static TownPromptTextCatalog GetFactCatalog() => ChinesePresentation.Value;

	internal static bool UseEnglishPrompt => !PromptLanguage.IsChinese;

	private static TownPromptTextCatalog LoadPresentation(string language)

	{

		return GcczLocalizedResourceLoader.Load(

			"GcczTownPrompt." + language + ".json",

			"AnimusForge.Defaults.GcczTownPrompt." + language + ".json",

			"GcczPrompt",

			Deserialize,

			TownPromptTextCatalog.CreateEnglishFallback);

	}



	private static TownPromptTextCatalog Deserialize(string json)

	{

		TownPromptTextCatalog parsed = JsonConvert.DeserializeObject<TownPromptTextCatalog>(json ?? string.Empty);

		return TownPromptTextCatalog.Resolve(parsed);

	}

}

