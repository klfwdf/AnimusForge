using Helpers;
using NavalDLC.View.GameMenus;
using NavalDLC.ViewModelCollection;
using SandBox.View.Map;
using SandBox.View.Menu;
using SandBox.ViewModelCollection;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View.Screens;

namespace NavalDLC.View.Map;

[GameStateScreen(typeof(MapState))]
public class NavalMapScreen : MapScreen
{
	public NavalMapScreen(MapState mapState)
		: base(mapState)
	{
	}

	protected override bool TickNavigationInput(float dt)
	{
		if (base.TickNavigationInput(dt))
		{
			return true;
		}
		if (base.SceneLayer.Input.IsGameKeyPressed(45) && base.NavigationHandler.GetElement("manage_fleet").Permission.IsAuthorized)
		{
			OpenManageFleet();
			return true;
		}
		return false;
	}

	protected override SPScoreboardVM CreateSimulationScoreboardDatasource(BattleSimulation battleSimulation)
	{
		MapEvent mapEvent = battleSimulation.MapEvent;
		if ((mapEvent != null && mapEvent.IsNavalMapEvent) || MapEventHelper.IsNavalRaid(battleSimulation.MapEvent))
		{
			return NavalScoreboardVM.CreateSimulation(battleSimulation);
		}
		return base.CreateSimulationScoreboardDatasource(battleSimulation);
	}

	protected override MenuViewContext CreateMenuViewContext(MenuContext menuContext)
	{
		return new NavalMenuViewContext(this, menuContext);
	}

	private void OpenManageFleet()
	{
		if (Hero.MainHero != null && !Hero.MainHero.IsPrisoner && !Hero.MainHero.IsDead)
		{
			PortStateHelper.OpenAsManageFleet(new MBReadOnlyList<Ship>());
		}
	}
}
