namespace TaleWorlds.Core;

public interface IGameStateManagerListener
{
	void OnCreateState(GameState gameState);

	void OnPushState(GameState gameState, bool isTopGameState);

	void OnPopState(GameState gameState);

	void OnCleanStates();

	void OnSavedGameLoadFinished();
}
