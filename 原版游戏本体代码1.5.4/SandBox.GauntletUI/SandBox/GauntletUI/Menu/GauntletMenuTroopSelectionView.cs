using System;
using SandBox.View.Map;
using SandBox.View.Menu;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TroopSelection;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu;

[OverrideView(typeof(MenuTroopSelectionView))]
public class GauntletMenuTroopSelectionView : MenuView
{
	private readonly Action<TroopRoster> _onDone;

	private readonly TroopRoster _fullRoster;

	private readonly TroopRoster _initialSelections;

	private readonly Func<CharacterObject, bool> _changeChangeStatusOfTroop;

	private readonly int _maxSelectableTroopCount;

	private readonly int _minSelectableTroopCount;

	private GauntletLayer _layerAsGauntletLayer;

	private GameMenuTroopSelectionVM _dataSource;

	private GauntletMovieIdentifier _movie;

	public GauntletMenuTroopSelectionView(TroopRoster fullRoster, TroopRoster initialSelections, Func<CharacterObject, bool> canChangeStatusOfTroop, Action<TroopRoster> onDone, int maxSelectableTroopCount, int minSelectableTroopCount)
	{
		_onDone = onDone;
		_fullRoster = fullRoster;
		_initialSelections = initialSelections;
		_changeChangeStatusOfTroop = canChangeStatusOfTroop;
		_maxSelectableTroopCount = maxSelectableTroopCount;
		_minSelectableTroopCount = minSelectableTroopCount;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new GameMenuTroopSelectionVM(_fullRoster, _initialSelections, _changeChangeStatusOfTroop, OnDone, _maxSelectableTroopCount, _minSelectableTroopCount)
		{
			IsEnabled = true
		};
		_dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_dataSource.SetResetInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Reset"));
		base.Layer = new GauntletLayer("MapTroopSelection", 206);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericCampaignPanelsGameKeyCategory"));
		_movie = _layerAsGauntletLayer.LoadMovie("GameMenuTroopSelection", _dataSource);
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_layerAsGauntletLayer);
		base.MenuViewContext.AddLayer(base.Layer);
		if (ScreenManager.TopScreen is MapScreen mapScreen)
		{
			mapScreen.SetIsInHideoutTroopManage(isInHideoutTroopManage: true);
		}
	}

	private void OnDone(TroopRoster obj)
	{
		MapScreen.Instance.SetIsInHideoutTroopManage(isInHideoutTroopManage: false);
		base.MenuViewContext.CloseTroopSelection();
		_onDone?.DynamicInvokeWithLog(obj);
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
		GameMenuTroopSelectionVM dataSource = _dataSource;
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
