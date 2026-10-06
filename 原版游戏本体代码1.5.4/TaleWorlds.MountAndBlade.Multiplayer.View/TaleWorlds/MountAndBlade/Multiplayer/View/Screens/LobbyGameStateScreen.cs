using TaleWorlds.Core;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.Multiplayer.View.Screens;

[GameStateScreen(typeof(LobbyGameStateMatchmakerClient))]
[GameStateScreen(typeof(LobbyGameStatePlayerBasedCustomServer))]
public class LobbyGameStateScreen : ScreenBase, IGameStateListener
{
	public LobbyGameStateScreen(LobbyGameState lobbyGameState)
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
