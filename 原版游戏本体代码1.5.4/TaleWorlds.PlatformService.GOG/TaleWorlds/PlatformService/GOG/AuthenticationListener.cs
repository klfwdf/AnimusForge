using Galaxy.Api;
using TaleWorlds.Library;

namespace TaleWorlds.PlatformService.GOG;

public class AuthenticationListener : GlobalAuthListener
{
	private GOGPlatformServices _gogPlatformServices;

	public bool GotResult { get; private set; }

	public AuthenticationListener(GOGPlatformServices gogPlatformServices)
	{
		_gogPlatformServices = gogPlatformServices;
	}

	public override void OnAuthSuccess()
	{
		Debug.Print("Successfully signed in");
		GalaxyInstance.User().GetGalaxyID();
		GotResult = true;
	}

	public override void OnAuthFailure(FailureReason failureReason)
	{
		Debug.Print("Failed to sign in for reason " + failureReason);
		GotResult = true;
	}

	public override void OnAuthLost()
	{
		Debug.Print("Authorization lost");
		GotResult = true;
	}
}
