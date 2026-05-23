using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.GameOptions;
using TaleWorlds.MountAndBlade.ViewModelCollection.InitialMenu;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

[GameStateScreen(typeof(InitialState))]
public class GauntletInitialScreen : MBInitialScreenBase, IChatLogHandlerScreen
{
	private GauntletLayer _gauntletLayer;

	private GauntletLayer _gauntletBrightnessLayer;

	private GauntletLayer _gauntletExposureLayer;

	private InitialMenuVM _dataSource;

	private BrightnessOptionVM _brightnessOptionDataSource;

	private ExposureOptionVM _exposureOptionDataSource;

	private GauntletMovieIdentifier _brightnessOptionMovie;

	private GauntletMovieIdentifier _exposureOptionMovie;

	public GauntletInitialScreen(InitialState initialState)
		: base(initialState)
	{
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new InitialMenuVM(base._state);
		_gauntletLayer = new GauntletLayer("MainMenu", 1);
		_gauntletLayer.LoadMovie("InitialScreen", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Mouse);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		AddLayer(_gauntletLayer);
		_gauntletLayer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(_gauntletLayer);
		if (NativeOptions.GetConfig(NativeOptions.NativeOptionsType.BrightnessCalibrated) < 4f)
		{
			_brightnessOptionDataSource = new BrightnessOptionVM(OnCloseBrightness)
			{
				Visible = true
			};
			_gauntletBrightnessLayer = new GauntletLayer("MainMenuBrightness", 2);
			_gauntletBrightnessLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Mouse);
			_brightnessOptionMovie = _gauntletBrightnessLayer.LoadMovie("BrightnessOption", _brightnessOptionDataSource);
			AddLayer(_gauntletBrightnessLayer);
		}
		GauntletFullScreenNoticeView.Initialize();
		GauntletGameNotification.Initialize();
		GauntletChatLogView.Current?.LoadMovie(forMultiplayer: false);
		InformationManager.ClearAllMessages();
		base._state.OnGameContentUpdated += OnGameContentUpdated;
		SetGainNavigationAfterFrames(3);
	}

	protected override void OnInitialScreenTick(float dt)
	{
		base.OnInitialScreenTick(dt);
		if (ScreenManager.IsMouseCursorHidden())
		{
			MouseManager.ShowCursor(show: false);
			MouseManager.ShowCursor(show: true);
		}
		_dataSource?.Tick();
		if (_gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			BrightnessOptionVM brightnessOptionDataSource = _brightnessOptionDataSource;
			if (brightnessOptionDataSource != null && brightnessOptionDataSource.Visible)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_brightnessOptionDataSource.ExecuteCancel();
				return;
			}
			ExposureOptionVM exposureOptionDataSource = _exposureOptionDataSource;
			if (exposureOptionDataSource != null && exposureOptionDataSource.Visible)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_exposureOptionDataSource.ExecuteCancel();
			}
		}
		else
		{
			if (!_gauntletLayer.Input.IsHotKeyReleased("Confirm"))
			{
				return;
			}
			BrightnessOptionVM brightnessOptionDataSource2 = _brightnessOptionDataSource;
			if (brightnessOptionDataSource2 != null && brightnessOptionDataSource2.Visible)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_brightnessOptionDataSource.ExecuteConfirm();
				return;
			}
			ExposureOptionVM exposureOptionDataSource2 = _exposureOptionDataSource;
			if (exposureOptionDataSource2 != null && exposureOptionDataSource2.Visible)
			{
				UISoundsHelper.PlayUISound("event:/ui/default");
				_exposureOptionDataSource.ExecuteConfirm();
			}
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_dataSource?.RefreshMenuOptions();
		SetGainNavigationAfterFrames(3);
	}

	private void SetGainNavigationAfterFrames(int frameCount)
	{
		_gauntletLayer.UIContext.GamepadNavigation.GainNavigationAfterFrames(frameCount, delegate
		{
			BrightnessOptionVM brightnessOptionDataSource = _brightnessOptionDataSource;
			if (brightnessOptionDataSource == null || !brightnessOptionDataSource.Visible)
			{
				ExposureOptionVM exposureOptionDataSource = _exposureOptionDataSource;
				if (exposureOptionDataSource == null)
				{
					return true;
				}
				return !exposureOptionDataSource.Visible;
			}
			return false;
		});
	}

	private void OnGameContentUpdated()
	{
		_dataSource?.RefreshMenuOptions();
	}

	private void OnCloseBrightness(bool isConfirm)
	{
		_gauntletBrightnessLayer.ReleaseMovie(_brightnessOptionMovie);
		RemoveLayer(_gauntletBrightnessLayer);
		_brightnessOptionDataSource = null;
		_gauntletBrightnessLayer = null;
		NativeOptions.SaveConfig();
		OpenExposureControl();
	}

	private void OpenExposureControl()
	{
		_exposureOptionDataSource = new ExposureOptionVM(OnCloseExposureControl)
		{
			Visible = true
		};
		_gauntletExposureLayer = new GauntletLayer("MainMenuExposure", 2);
		_gauntletExposureLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: true, InputUsageMask.Mouse);
		_exposureOptionMovie = _gauntletExposureLayer.LoadMovie("ExposureOption", _exposureOptionDataSource);
		AddLayer(_gauntletExposureLayer);
	}

	private void OnCloseExposureControl(bool isConfirm)
	{
		_gauntletExposureLayer.ReleaseMovie(_exposureOptionMovie);
		RemoveLayer(_gauntletExposureLayer);
		_exposureOptionDataSource = null;
		_gauntletExposureLayer = null;
		NativeOptions.SaveConfig();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (base._state != null)
		{
			base._state.OnGameContentUpdated -= OnGameContentUpdated;
		}
		if (_gauntletLayer != null)
		{
			RemoveLayer(_gauntletLayer);
		}
		_dataSource?.OnFinalize();
		_dataSource = null;
		_gauntletLayer = null;
	}

	public void TryUpdateChatLogLayerParameters(ref bool isTeamChatAvailable, ref bool inputEnabled, ref bool isToggleChatHintAvailable, ref bool isMouseVisible, ref InputContext inputContext)
	{
		inputEnabled = false;
		inputContext = null;
	}
}
