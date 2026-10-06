using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.ProfileSelection;
using TaleWorlds.PlatformService;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

[GameStateScreen(typeof(ProfileSelectionState))]
public class GauntletProfileSelectionScreen : MBProfileSelectionScreenBase
{
	private GauntletLayer _gauntletLayer;

	private ProfileSelectionVM _dataSource;

	private ProfileSelectionState _state;

	public GauntletProfileSelectionScreen(ProfileSelectionState state)
		: base(state)
	{
		_state = state;
		_state.OnProfileSelection += OnProfileSelection;
	}

	private void OnProfileSelection()
	{
		_dataSource?.OnActivate(_state.IsDirectPlayPossible);
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_gauntletLayer = new GauntletLayer("ProfileSelection", 1);
		_dataSource = new ProfileSelectionVM(_state.IsDirectPlayPossible);
		_dataSource?.OnActivate(_state.IsDirectPlayPossible);
		_gauntletLayer.LoadMovie("ProfileSelectionScreen", _dataSource);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		ScreenManager.TrySetFocus(_gauntletLayer);
		AddLayer(_gauntletLayer);
		MouseManager.ShowCursor(show: false);
		MouseManager.ShowCursor(show: true);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		_dataSource?.OnActivate(_state.IsDirectPlayPossible);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_state.OnProfileSelection -= OnProfileSelection;
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		RemoveLayer(_gauntletLayer);
		_gauntletLayer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	protected override void OnProfileSelectionTick(float dt)
	{
		base.OnProfileSelectionTick(dt);
		if (_state.IsDirectPlayPossible && _gauntletLayer.Input.IsHotKeyReleased("Play"))
		{
			if (PlatformServices.Instance.UserLoggedIn)
			{
				_state.StartGame();
			}
			else
			{
				OnActivateProfileSelection();
			}
		}
		else if (_gauntletLayer.Input.IsHotKeyReleased("SelectProfile"))
		{
			OnActivateProfileSelection();
		}
	}
}
