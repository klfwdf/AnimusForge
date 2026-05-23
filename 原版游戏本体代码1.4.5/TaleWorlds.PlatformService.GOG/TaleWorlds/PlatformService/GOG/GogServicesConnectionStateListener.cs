using Galaxy.Api;
using TaleWorlds.Library;

namespace TaleWorlds.PlatformService.GOG;

public class GogServicesConnectionStateListener : GlobalGogServicesConnectionStateListener
{
	public override void OnConnectionStateChange(GogServicesConnectionState connected)
	{
		Debug.Print("Connection state to GOG services changed to " + connected);
	}
}
