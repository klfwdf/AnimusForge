using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace SandBox.View.Menu;

[GameStateScreen(typeof(TutorialState))]
public class TutorialScreen : ScreenBase, IGameStateListener
{
	public MenuViewContext MenuViewContext { get; }

	public TutorialScreen(TutorialState tutorialState)
	{
		MenuViewContext = new MenuViewContext(this, tutorialState.MenuContext);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		MenuViewContext.OnFrameTick(dt);
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		MenuViewContext.OnActivate();
		LoadingWindow.DisableGlobalLoadingWindow();
	}

	protected override void OnDeactivate()
	{
		MenuViewContext.OnDeactivate();
		base.OnDeactivate();
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		MenuViewContext.OnInitialize();
	}

	protected override void OnFinalize()
	{
		MenuViewContext.OnFinalize();
		base.OnFinalize();
	}

	void IGameStateListener.OnActivate()
	{
	}

	void IGameStateListener.OnDeactivate()
	{
		MenuViewContext.OnGameStateDeactivate();
	}

	void IGameStateListener.OnInitialize()
	{
		MenuViewContext.OnGameStateInitialize();
	}

	void IGameStateListener.OnFinalize()
	{
		MenuViewContext.OnGameStateFinalize();
	}
}
