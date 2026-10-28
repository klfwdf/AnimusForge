using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public abstract class MultiplayerGameMode
{
	public string Name { get; private set; }

	protected MultiplayerGameMode(string name)
	{
		Name = name;
	}

	public abstract void JoinCustomGame(JoinGameData joinGameData);

	public abstract void StartMultiplayerGame(string scene);
}
