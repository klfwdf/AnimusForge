using System.Threading.Tasks;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

public interface ICustomBattleServerSessionHandler
{
	void OnConnected();

	void OnCantConnect();

	void OnDisconnected();

	void OnStateChanged(CustomBattleServer.State state);

	void OnSuccessfulGameRegister();

	Task<PlayerJoinGameResponseDataFromHost[]> OnClientWantsToConnectCustomGame(PlayerJoinGameData[] playerJoinData);

	void OnClientQuitFromCustomGame(PlayerId playerId);

	void OnGameFinished();

	void OnChatFilterListsReceived(string[] profanityList, string[] allowList);

	void OnPlayerKickRequested(PlayerId playerID, bool isBanning);
}
