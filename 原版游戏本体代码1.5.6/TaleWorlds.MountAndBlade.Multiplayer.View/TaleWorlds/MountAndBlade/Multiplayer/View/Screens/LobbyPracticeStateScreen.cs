using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.Screens;

[GameStateScreen(typeof(LobbyPracticeState))]
public class LobbyPracticeStateScreen : ScreenBase, IGameStateListener
{
	public LobbyPracticeStateScreen(LobbyPracticeState lobbyPracticeState)
	{
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
