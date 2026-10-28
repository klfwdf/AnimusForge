namespace TaleWorlds.Core;

public interface IGameStateManagerOwner
{
	void OnStateStackEmpty();

	void OnStateChanged(GameState oldState);
}
