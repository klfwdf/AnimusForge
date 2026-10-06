using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI;

[GameStateScreen(typeof(BarberState))]
public class GauntletBarberScreen : ScreenBase, IGameStateListener, IFaceGeneratorScreen
{
	private readonly BodyGeneratorView _facegenLayer;

	public IFaceGeneratorHandler Handler => _facegenLayer;

	public GauntletBarberScreen(BarberState state)
	{
		LoadingWindow.EnableGlobalLoadingWindow();
		_facegenLayer = new BodyGeneratorView(OnExit, GameTexts.FindText("str_done"), OnExit, GameTexts.FindText("str_cancel"), Hero.MainHero.CharacterObject, openedFromMultiplayer: false, state.Filter);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		_facegenLayer.OnTick(dt);
	}

	public void OnExit()
	{
		Game.Current.GameStateManager.PopState();
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		Game.Current.GameStateManager.RegisterActiveStateDisableRequest(this);
		AddLayer(_facegenLayer.GauntletLayer);
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (LoadingWindow.IsLoadingWindowActive)
		{
			LoadingWindow.DisableGlobalLoadingWindow();
		}
		Game.Current.GameStateManager.UnregisterActiveStateDisableRequest(this);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		AddLayer(_facegenLayer.SceneLayer);
	}

	protected override void OnDeactivate()
	{
		base.OnDeactivate();
		_facegenLayer.SceneLayer.SceneView.SetEnable(value: false);
		_facegenLayer.OnFinalize();
		LoadingWindow.EnableGlobalLoadingWindow();
		MBInformationManager.HideInformations();
	}

	void IGameStateListener.OnActivate()
	{
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
