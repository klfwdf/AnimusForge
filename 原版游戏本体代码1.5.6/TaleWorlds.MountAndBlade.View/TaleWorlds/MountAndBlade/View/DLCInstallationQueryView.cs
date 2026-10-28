using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.MountAndBlade.View;

public class DLCInstallationQueryView
{
	public void Initialize()
	{
		EngineController.OnDLCInstalledCallback += OnModuleInstallComplete;
		EngineController.OnDLCLoadedCallback += OnModuleActivated;
	}

	private void OnModuleActivated()
	{
		MBInformationManager.AddQuickInformation(Module.CurrentModule.GlobalTextManager.FindText("str_content_activated_notification"), 1000);
		if (Module.CurrentModule.GlobalGameStateManager.ActiveState is InitialState initialState)
		{
			initialState.RefreshContentState();
		}
	}

	private void OnModuleInstallComplete()
	{
		MBInformationManager.AddQuickInformation(Module.CurrentModule.GlobalTextManager.FindText("str_content_installed_notification"), 1000);
		if (!(Module.CurrentModule.GlobalGameStateManager.ActiveState is InitialState))
		{
			CreateInstallationCompleteQuery();
		}
	}

	private void CreateInstallationCompleteQuery()
	{
		GetQueryTexts(out var title, out var description);
		InformationManager.ShowInquiry(new InquiryData(title, description, isAffirmativeOptionShown: true, isNegativeOptionShown: false, new TextObject("{=yS7PvrTD}OK").ToString(), null, null, null));
	}

	private void GetQueryTexts(out string title, out string description)
	{
		title = Module.CurrentModule.GlobalTextManager.FindText("str_dlc_installed_title").ToString();
		description = Module.CurrentModule.GlobalTextManager.FindText("str_dlc_installed_description").ToString();
	}

	public void OnFinalize()
	{
		EngineController.OnDLCInstalledCallback -= OnModuleInstallComplete;
		EngineController.OnDLCLoadedCallback -= OnModuleActivated;
	}
}
