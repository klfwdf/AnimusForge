using NavalDLC.View.Map.Navigation;
using SandBox.GauntletUI.Map;
using SandBox.View.Map;
using TaleWorlds.CampaignSystem;
using TaleWorlds.InputSystem;

namespace NavalDLC.GauntletUI.Map;

public class GauntletNavalMapBarGlobalLayer : GauntletMapBarGlobalLayer
{
	private readonly ManageFleetNavigationElement _manageFleetNavigationElement;

	public GauntletNavalMapBarGlobalLayer(MapScreen mapScreen, INavigationHandler navigationHandler, float contextAlphaModifider)
		: base(mapScreen, navigationHandler, contextAlphaModifider)
	{
		_manageFleetNavigationElement = (navigationHandler as NavalMapNavigationHandler).ManageFleetNavigationElement;
	}

	protected override bool HandlePanelSwitchingInput(InputContext inputContext)
	{
		if (base.HandlePanelSwitchingInput(inputContext))
		{
			return true;
		}
		if (inputContext.IsGameKeyReleased(45) && !_manageFleetNavigationElement.IsActive)
		{
			_manageFleetNavigationElement.OpenView();
			return true;
		}
		return false;
	}
}
