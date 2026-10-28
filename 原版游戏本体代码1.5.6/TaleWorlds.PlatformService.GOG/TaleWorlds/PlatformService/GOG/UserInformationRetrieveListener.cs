using Galaxy.Api;

namespace TaleWorlds.PlatformService.GOG;

public class UserInformationRetrieveListener : IUserInformationRetrieveListener
{
	public bool GotResult { get; private set; }

	public override void OnUserInformationRetrieveFailure(GalaxyID userID, FailureReason failureReason)
	{
		GotResult = true;
	}

	public override void OnUserInformationRetrieveSuccess(GalaxyID userID)
	{
		GotResult = true;
	}
}
