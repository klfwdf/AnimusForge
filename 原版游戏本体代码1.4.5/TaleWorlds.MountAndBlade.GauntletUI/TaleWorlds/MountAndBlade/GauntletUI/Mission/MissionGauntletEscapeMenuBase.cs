using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.ViewModelCollection.EscapeMenu;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.Mission;

public abstract class MissionGauntletEscapeMenuBase : MissionEscapeMenuView
{
	protected EscapeMenuVM DataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _movie;

	private string _viewFile;

	private bool _isRenderingStarted;

	private TutorialContexts _escapeMenuPrevTutorialContext;

	protected MissionGauntletEscapeMenuBase(string viewFile)
	{
		base.OnMissionScreenInitialize();
		_viewFile = viewFile;
		ViewOrderPriority = 50;
		Game.Current.EventManager.RegisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
	}

	protected virtual List<EscapeMenuItemVM> GetEscapeMenuItems()
	{
		return null;
	}

	public override void OnMissionScreenFinalize()
	{
		Game.Current.EventManager.UnregisterEvent<TutorialContextChangedEvent>(OnTutorialContextChanged);
		DataSource.OnFinalize();
		DataSource = null;
		_gauntletLayer = null;
		_movie = null;
		base.OnMissionScreenFinalize();
	}

	public override bool OnEscape()
	{
		if (!_isRenderingStarted)
		{
			return false;
		}
		if (!base.IsActive)
		{
			DataSource.RefreshItems(GetEscapeMenuItems());
		}
		return OnEscapeMenuToggled(!base.IsActive);
	}

	protected bool OnEscapeMenuToggled(bool isOpened)
	{
		if (base.IsActive == isOpened)
		{
			return false;
		}
		base.IsActive = isOpened;
		if (isOpened)
		{
			Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(TutorialContexts.EscapeMenu));
			DataSource.RefreshValues();
			if (!GameNetwork.IsMultiplayer)
			{
				MBCommon.PauseGameEngine();
				Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
			}
			_gauntletLayer = new GauntletLayer("MissionEscapeMenu", ViewOrderPriority);
			_gauntletLayer.IsFocusLayer = true;
			_gauntletLayer.InputRestrictions.SetInputRestrictions();
			_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			_movie = _gauntletLayer.LoadMovie(_viewFile, DataSource);
			base.MissionScreen.AddLayer(_gauntletLayer);
			ScreenManager.TrySetFocus(_gauntletLayer);
		}
		else
		{
			Game.Current.EventManager.TriggerEvent(new TutorialContextChangedEvent(_escapeMenuPrevTutorialContext));
			if (!GameNetwork.IsMultiplayer)
			{
				MBCommon.UnPauseGameEngine();
				Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
			}
			_gauntletLayer.InputRestrictions.ResetInputRestrictions();
			base.MissionScreen.RemoveLayer(_gauntletLayer);
			_movie = null;
			_gauntletLayer = null;
		}
		return true;
	}

	public override void OnMissionScreenTick(float dt)
	{
		base.OnMissionScreenTick(dt);
		if (base.IsActive && (_gauntletLayer.Input.IsHotKeyReleased("ToggleEscapeMenu") || _gauntletLayer.Input.IsHotKeyReleased("Exit")))
		{
			OnEscapeMenuToggled(isOpened: false);
		}
	}

	public override void OnSceneRenderingStarted()
	{
		base.OnSceneRenderingStarted();
		_isRenderingStarted = true;
	}

	private void OnTutorialContextChanged(TutorialContextChangedEvent obj)
	{
		if (obj.NewContext != TutorialContexts.EscapeMenu)
		{
			_escapeMenuPrevTutorialContext = obj.NewContext;
		}
	}
}
