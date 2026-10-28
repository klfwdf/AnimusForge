using System;
using System.Collections.Generic;
using NavalDLC.View.GameMenus;
using NavalDLC.ViewModelCollection.GameMenus;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace NavalDLC.GauntletUI.Menu;

[OverrideView(typeof(NavalMenuTroopSelectionView))]
public class GauntletNavalMenuTroopSelectionView : MenuView
{
	private readonly Action<TroopRoster, List<Ship>> _onDone;

	private readonly TroopRoster _fullRoster;

	private readonly TroopRoster _initialTroopSelections;

	private readonly Func<CharacterObject, bool> _changeChangeStatusOfTroop;

	private readonly int _minSelectableTroopCount;

	private readonly int _minSelectableShipCount;

	private readonly int _maxSelectableShipCount;

	private readonly List<Ship> _eligibleShips;

	private readonly List<Ship> _initialShipSelections;

	private readonly bool _anyOtherPartiesOnPlayerSide;

	private GauntletLayer _layerAsGauntletLayer;

	private NavalGameMenuTroopSelectionVM _dataSource;

	private GauntletMovieIdentifier _movie;

	public GauntletNavalMenuTroopSelectionView(TroopRoster fullRoster, TroopRoster initialTroopSelections, List<Ship> eligibleShips, List<Ship> initialShipSelections, Func<CharacterObject, bool> changeChangeStatusOfTroop, Action<TroopRoster, List<Ship>> onDone, int minSelectableTroopCount, int minSelectableShipCount, int maxSelectableShipCount, bool anyOtherPartiesOnPlayerSide)
	{
		_onDone = onDone;
		_fullRoster = fullRoster;
		_initialTroopSelections = initialTroopSelections;
		_changeChangeStatusOfTroop = changeChangeStatusOfTroop;
		_minSelectableTroopCount = minSelectableTroopCount;
		_minSelectableShipCount = minSelectableShipCount;
		_maxSelectableShipCount = maxSelectableShipCount;
		_eligibleShips = eligibleShips;
		_initialShipSelections = initialShipSelections;
		_anyOtherPartiesOnPlayerSide = anyOtherPartiesOnPlayerSide;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new NavalGameMenuTroopSelectionVM(_fullRoster, _initialTroopSelections, _eligibleShips, _initialShipSelections, _changeChangeStatusOfTroop, OnDone, _minSelectableTroopCount, _minSelectableShipCount, _maxSelectableShipCount, _anyOtherPartiesOnPlayerSide)
		{
			IsEnabled = true
		};
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		base.Layer = new GauntletLayer("NavalMapTroopSelection", 206);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_movie = _layerAsGauntletLayer.LoadMovie("NavalGameMenuTroopSelection", _dataSource);
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layerAsGauntletLayer);
		base.MenuViewContext.AddLayer(base.Layer);
		if (ScreenManager.TopScreen is MapScreen mapScreen)
		{
			mapScreen.SetIsInHideoutTroopManage(isInHideoutTroopManage: true);
		}
	}

	private void OnDone(TroopRoster troops, List<Ship> ships)
	{
		MapScreen.Instance.SetIsInHideoutTroopManage(isInHideoutTroopManage: false);
		base.MenuViewContext.CloseTroopSelection();
		_onDone?.DynamicInvokeWithLog(troops, ships);
	}

	protected override void OnFinalize()
	{
		base.Layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(base.Layer);
		_dataSource.OnFinalize();
		_dataSource = null;
		_layerAsGauntletLayer.ReleaseMovie(_movie);
		base.MenuViewContext.RemoveLayer(base.Layer);
		_movie = null;
		base.Layer = null;
		_layerAsGauntletLayer = null;
		MapScreen.Instance.SetIsInHideoutTroopManage(isInHideoutTroopManage: false);
		base.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_dataSource != null)
		{
			_dataSource.IsFiveStackModifierActive = base.Layer.Input.IsHotKeyDown("FiveStackModifier");
			_dataSource.IsEntireStackModifierActive = base.Layer.Input.IsHotKeyDown("EntireStackModifier");
		}
		ScreenLayer layer = base.Layer;
		if (layer != null && layer.Input.IsHotKeyPressed("Exit"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.ExecuteCancel();
		}
		else
		{
			ScreenLayer layer2 = base.Layer;
			if (layer2 != null && layer2.Input.IsHotKeyPressed("Confirm") && _dataSource.IsDoneEnabled)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_dataSource.ExecuteDone();
			}
			else
			{
				ScreenLayer layer3 = base.Layer;
				if (layer3 != null && layer3.Input.IsHotKeyPressed("Reset"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					_dataSource.ExecuteReset();
				}
			}
		}
		NavalGameMenuTroopSelectionVM dataSource = _dataSource;
		if (dataSource != null && !dataSource.IsEnabled)
		{
			base.MenuViewContext.CloseTroopSelection();
		}
	}

	protected override void OnMapConversationActivated()
	{
		base.OnMapConversationActivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: true);
		}
	}

	protected override void OnMapConversationDeactivated()
	{
		base.OnMapConversationDeactivated();
		if (_layerAsGauntletLayer != null)
		{
			ScreenManager.SetSuspendLayer(_layerAsGauntletLayer, isSuspended: false);
		}
	}
}
