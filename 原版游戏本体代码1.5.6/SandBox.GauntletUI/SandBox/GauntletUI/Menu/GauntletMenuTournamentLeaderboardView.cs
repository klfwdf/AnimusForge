using SandBox.View.Menu;
using TaleWorlds.CampaignSystem.ViewModelCollection.GameMenu.TournamentLeaderboard;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Menu;

[OverrideView(typeof(MenuTournamentLeaderboardView))]
public class GauntletMenuTournamentLeaderboardView : MenuView
{
	private GauntletLayer _layerAsGauntletLayer;

	private TournamentLeaderboardVM _dataSource;

	private GauntletMovieIdentifier _movie;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_dataSource = new TournamentLeaderboardVM
		{
			IsEnabled = true
		};
		base.Layer = new GauntletLayer("MapTournamentLeaderboard", 206);
		_layerAsGauntletLayer = base.Layer as GauntletLayer;
		base.Layer.InputRestrictions.SetInputRestrictions();
		base.Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
		_movie = _layerAsGauntletLayer.LoadMovie("GameMenuTournamentLeaderboard", _dataSource);
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		base.MenuViewContext.AddLayer(base.Layer);
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
		base.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (base.Layer.Input.IsHotKeyReleased("Exit") || base.Layer.Input.IsHotKeyReleased("Confirm"))
		{
			UISoundsHelper.PlayUISound("event:/ui/default");
			_dataSource.IsEnabled = false;
		}
		if (!_dataSource.IsEnabled)
		{
			base.MenuViewContext.CloseTournamentLeaderboard();
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
