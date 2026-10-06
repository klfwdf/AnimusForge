using Galaxy.Api;

namespace TaleWorlds.Diamond.AccessProvider.GOG;

internal class EncryptedAppTicketListener : IEncryptedAppTicketListener
{
	public bool GotResult { get; private set; }

	public override void OnEncryptedAppTicketRetrieveFailure(FailureReason failureReason)
	{
		GotResult = true;
	}

	public override void OnEncryptedAppTicketRetrieveSuccess()
	{
		GotResult = true;
	}
}
