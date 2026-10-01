using System;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

/// <summary>
/// Presents the resource-driven town manual through the native inquiry UI.
/// </summary>
internal static class GcczTownManualInquiryPresenter
{
	internal static void Open()
	{
		try
		{
			ShowPage(GcczTownManualResourceProvider.GetCatalog(), 0);
		}
		catch (Exception ex)
		{
			Logger.Log("GcczManual", "Town manual presentation failed. Error=" + ex.Message);
			ShowFailure();
		}
	}

	private static void ShowPage(TownManualCatalog source, int requestedIndex)
	{
		TownManualCatalog catalog = TownManualCatalog.Resolve(source);
		TownManualPageView view = catalog.GetPage(requestedIndex);
		bool isLastPage = !view.HasNext;
		bool showNegativeOption = view.HasPrevious || view.HasNext;
		Action affirmativeAction = isLastPage
			? null
			: delegate { ShowPage(catalog, view.Index + 1); };
		Action negativeAction = view.HasPrevious
			? delegate { ShowPage(catalog, view.Index - 1); }
			: null;

		InformationManager.ShowInquiry(
			new InquiryData(
				view.WindowTitle,
				view.Page.Body,
				isAffirmativeOptionShown: true,
				isNegativeOptionShown: showNegativeOption,
				isLastPage ? catalog.CloseButtonText : catalog.NextButtonText,
				view.HasPrevious ? catalog.PreviousButtonText : catalog.CloseButtonText,
				affirmativeAction,
				negativeAction),
			pauseGameActiveState: true);
	}

	private static void ShowFailure()
	{
		try
		{
			TownManualCatalog fallback = TownManualCatalog.CreateEnglishFallback();
			InformationManager.ShowInquiry(
				new InquiryData(
					fallback.ManualTitle,
					fallback.OpenFailedMessage,
					isAffirmativeOptionShown: true,
					isNegativeOptionShown: false,
					fallback.CloseButtonText,
					string.Empty,
					null,
					null),
				pauseGameActiveState: true);
		}
		catch (Exception ex)
		{
			Logger.Log("GcczManual", "Town manual fail-safe inquiry failed. Error=" + ex.Message);
		}
	}
}
