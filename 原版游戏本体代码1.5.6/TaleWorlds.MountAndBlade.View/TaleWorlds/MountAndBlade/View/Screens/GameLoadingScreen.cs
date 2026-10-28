using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.View.Screens;

[GameStateScreen(typeof(GameLoadingState))]
public class GameLoadingScreen : ScreenBase, IGameStateListener
{
	public GameLoadingScreen(GameLoadingState gameLoadingState)
	{
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		LoadingWindow.EnableGlobalLoadingWindow();
		Utilities.SetScreenTextRenderingState(state: false);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		Utilities.SetScreenTextRenderingState(state: true);
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
