namespace TaleWorlds.MountAndBlade.Diamond;

public interface IBattleServerSessionHandler
{
	void OnConnected();

	void OnCantConnect();

	void OnDisconnected();

	void OnNewPlayer(BattlePeer peer);

	void OnStartGame(string sceneName, string gameType, string faction1, string faction2, int minRequiredPlayerCountToStartBattle, int battleSize, string[] profanityList, string[] allowList);

	void OnPlayerFledBattle(BattlePeer peer, out BattleResult battleResult, bool isQuitFromBattle);

	void OnEndMission();

	void OnStopServer();
}
