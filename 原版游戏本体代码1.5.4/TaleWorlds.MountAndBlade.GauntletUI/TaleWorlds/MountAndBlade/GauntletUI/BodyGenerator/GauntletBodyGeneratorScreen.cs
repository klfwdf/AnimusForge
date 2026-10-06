using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI.BodyGenerator;

[OverrideView(typeof(FaceGeneratorScreen))]
public class GauntletBodyGeneratorScreen : ScreenBase, IFaceGeneratorScreen
{
	private const int ViewOrderPriority = 15;

	private readonly BodyGeneratorView _facegenLayer;

	public IFaceGeneratorHandler Handler => _facegenLayer;

	public GauntletBodyGeneratorScreen(BasicCharacterObject character, bool openedFromMultiplayer, IFaceGeneratorCustomFilter filter)
	{
		_facegenLayer = new BodyGeneratorView(OnExit, GameTexts.FindText("str_done"), OnExit, GameTexts.FindText("str_cancel"), character, openedFromMultiplayer, filter);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		_facegenLayer.OnTick(dt);
	}

	public void OnExit()
	{
		ScreenManager.PopScreen();
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
		_facegenLayer.OnFinalize();
		LoadingWindow.EnableGlobalLoadingWindow();
		MBInformationManager.HideInformations();
		TaleWorlds.MountAndBlade.Mission current = TaleWorlds.MountAndBlade.Mission.Current;
		if (current == null)
		{
			return;
		}
		foreach (Agent agent in current.Agents)
		{
			agent.EquipItemsFromSpawnEquipment(neededBatchedItems: false, prepareImmediately: false, useFaceCache: false, 0);
			agent.UpdateAgentProperties();
		}
	}
}
