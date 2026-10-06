using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.MountAndBlade.Multiplayer.ViewModelCollection;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.GauntletUI;

[GameStateScreen(typeof(LobbyPracticeState))]
public class LobbyPracticeStateGauntletScreen : ScreenBase, IGameStateListener
{
	private MPPracticeVM _dataSource;

	public GauntletLayer Layer { get; private set; }

	public LobbyPracticeStateGauntletScreen(LobbyPracticeState gameState)
	{
		_dataSource = new MPPracticeVM();
		Layer = new GauntletLayer("LobbyPracticeScreen", 100);
		Layer.IsFocusLayer = true;
		AddLayer(Layer);
		Layer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		Layer.LoadMovie("MultiplayerPractice", _dataSource);
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		Layer.InputRestrictions.ResetInputRestrictions();
		Layer = null;
		_dataSource.OnFinalize();
		_dataSource = null;
	}

	void IGameStateListener.OnActivate()
	{
		Layer.InputRestrictions.SetInputRestrictions();
		ScreenManager.TrySetFocus(Layer);
	}

	void IGameStateListener.OnDeactivate()
	{
	}

	void IGameStateListener.OnInitialize()
	{
	}

	void IGameStateListener.OnFinalize()
	{
	}
}
