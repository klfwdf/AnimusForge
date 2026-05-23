using Galaxy.Api;

namespace TaleWorlds.PlatformService.GOG;

public class FriendListListener : IFriendListListener
{
	public bool GotResult { get; private set; }

	public override void OnFriendListRetrieveSuccess()
	{
		GotResult = true;
	}

	public override void OnFriendListRetrieveFailure(FailureReason failureReason)
	{
		GotResult = true;
	}
}
