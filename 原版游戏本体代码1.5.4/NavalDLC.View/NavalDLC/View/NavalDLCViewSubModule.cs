using System;
using NavalDLC.HotKeyCategories;
using NavalDLC.View.Map;
using NavalDLC.View.Map.Managers;
using NavalDLC.View.Missions;
using NavalDLC.View.Overlay;
using NavalDLC.View.Permissions;
using NavalDLC.View.VisualOrders;
using SandBox;
using SandBox.View;
using SandBox.View.Map;
using SandBox.ViewModelCollection.Missions.NameMarker;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.Overlay;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ViewModelCollection.Order.Visual;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.View;

public class NavalDLCViewSubModule : MBSubModuleBase
{
	private NavalRaidVisualOrderProvider _raidVisualOrderProvider;

	private NavalShipVisualOrderProvider _shipVisualOrderProvider;

	private NavalTroopVisualOrderProvider _troopVisualOrderProvider;

	private NavalGameMenuOverlayProvider _gameMenuOverlayProvider;

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		RegisterHotKeyContexts();
		RegisterTooltipTypes();
		_raidVisualOrderProvider = new NavalRaidVisualOrderProvider();
		_shipVisualOrderProvider = new NavalShipVisualOrderProvider();
		_troopVisualOrderProvider = new NavalTroopVisualOrderProvider();
		VisualOrderFactory.RegisterProvider(_raidVisualOrderProvider);
		VisualOrderFactory.RegisterProvider(_shipVisualOrderProvider);
		VisualOrderFactory.RegisterProvider(_troopVisualOrderProvider);
		_gameMenuOverlayProvider = new NavalGameMenuOverlayProvider();
		GameMenuOverlayFactory.RegisterProvider(_gameMenuOverlayProvider);
		MissionNameMarkerFactory.DefaultContext.AddProvider<NavalMissionNameMarkerProvider>();
		ScreenManager.OnPushScreen += OnScreenPushed;
	}

	public override void OnNewGameCreated(Game game, object initializerObject)
	{
		if (game.GameType is Campaign)
		{
			NavalDLCManager.Instance.NavalMapSceneWrapper = new NavalMapSceneWrapper();
		}
	}

	public override void OnAfterGameLoaded(Game game)
	{
		if (game.GameType is Campaign)
		{
			NavalDLCManager.Instance.NavalMapSceneWrapper = new NavalMapSceneWrapper();
		}
	}

	public override void OnGameInitializationFinished(Game game)
	{
		base.OnGameInitializationFinished(game);
		NavalPermissionsSystem.OnInitialize();
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		NavalPermissionsSystem.OnUnload();
		VisualShipFactory.DeregisterVisualShipCache();
	}

	protected override void OnSubModuleUnloaded()
	{
		base.OnSubModuleUnloaded();
		UnregisterTooltipTypes();
		VisualOrderFactory.UnregisterProvider(_raidVisualOrderProvider);
		VisualOrderFactory.UnregisterProvider(_shipVisualOrderProvider);
		VisualOrderFactory.UnregisterProvider(_troopVisualOrderProvider);
		GameMenuOverlayFactory.UnregisterProvider(_gameMenuOverlayProvider);
		ScreenManager.OnPushScreen -= OnScreenPushed;
	}

	public override void OnSubModuleDeactivated()
	{
	}

	public override void OnSubModuleActivated()
	{
	}

	private void RegisterTooltipTypes()
	{
		InformationManager.RegisterTooltip<Ship, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshShipTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<ShipHull, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshShipHullTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<ShipUpgradePiece, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshShipPieceTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Figurehead, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshFigureheadTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<AnchorPoint, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshAnchorPointTooltip, "PropertyBasedTooltip");
		InformationManager.RegisterTooltip<Settlement, PropertyBasedTooltipVM>(NavalTooltipRefresherCollection.RefreshSettlementTooltip, "PropertyBasedTooltip");
	}

	private void UnregisterTooltipTypes()
	{
		InformationManager.UnregisterTooltip<Ship>();
		InformationManager.UnregisterTooltip<ShipHull>();
		InformationManager.UnregisterTooltip<ShipUpgradePiece>();
		InformationManager.UnregisterTooltip<Figurehead>();
		InformationManager.UnregisterTooltip<AnchorPoint>();
		InformationManager.UnregisterTooltip<Settlement>();
	}

	private void RegisterHotKeyContexts()
	{
		HotKeyManager.RegisterContext(new NavalShipControlsHotKeyCategory());
		HotKeyManager.RegisterContext(new PortHotKeyCategory());
		HotKeyManager.RegisterContext(new NavalCheatsHotKeyCategory(), ignoreSerialize: true);
	}

	private void OnScreenPushed(ScreenBase pushedScreen)
	{
		if (pushedScreen is MapScreen mapScreen)
		{
			mapScreen.AddMapView<NavalMapAnchorTrackerView>(Array.Empty<object>());
		}
	}

	public override void OnAfterGameInitializationFinished(Game game, object starterObject)
	{
		base.OnAfterGameInitializationFinished(game, starterObject);
		if (Campaign.Current != null && Campaign.Current.MapSceneWrapper != null)
		{
			VisualShipFactory.InitializeShipEntityCache(((MapScene)Campaign.Current.MapSceneWrapper).Scene);
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<NavalMobilePartyVisualManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<AnchorVisualManager>();
			SandBoxViewSubModule.SandBoxViewVisualManager.AddEntityComponent<StormVisualManager>();
		}
	}
}
