using TaleWorlds.MountAndBlade.Diamond;

namespace TaleWorlds.MountAndBlade;

public interface ICommunityClientHandler
{
	void OnJoinCustomGameResponse(string address, int port, PlayerJoinGameResponseDataFromHost response);

	void OnQuitFromGame();
}
