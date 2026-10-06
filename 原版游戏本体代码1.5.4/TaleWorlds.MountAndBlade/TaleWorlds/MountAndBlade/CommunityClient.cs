using TaleWorlds.Library.Http;

namespace TaleWorlds.MountAndBlade;

public class CommunityClient
{
	private IHttpDriver _httpDriver;

	public bool IsInGame { get; private set; }

	public ICommunityClientHandler Handler { get; set; }

	public CommunityClient()
	{
		_httpDriver = HttpDriverManager.GetDefaultHttpDriver();
	}

	public void QuitFromGame()
	{
		if (IsInGame)
		{
			IsInGame = false;
			Handler?.OnQuitFromGame();
		}
	}
}
