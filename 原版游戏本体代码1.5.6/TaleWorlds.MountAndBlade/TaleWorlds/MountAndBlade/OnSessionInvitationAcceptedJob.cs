using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

internal class OnSessionInvitationAcceptedJob : Job
{
	private readonly SessionInvitationType _sessionInvitationType;

	public OnSessionInvitationAcceptedJob(SessionInvitationType sessionInvitationType)
	{
		_sessionInvitationType = sessionInvitationType;
	}

	public override void DoJob(float dt)
	{
		base.DoJob(dt);
		if (MBGameManager.Current != null)
		{
			MBGameManager.Current.OnSessionInvitationAccepted(_sessionInvitationType);
		}
		else if (GameStateManager.Current != null && GameStateManager.Current.ActiveState != null)
		{
			GameStateManager.Current.CleanStates();
		}
		base.Finished = true;
	}
}
