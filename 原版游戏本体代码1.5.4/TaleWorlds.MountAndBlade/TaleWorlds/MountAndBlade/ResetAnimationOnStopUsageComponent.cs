using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class ResetAnimationOnStopUsageComponent : UsableMissionObjectComponent
{
	private ActionIndexCache _successfulResetAction;

	private readonly bool _alwaysResetWithAction;

	public ResetAnimationOnStopUsageComponent(ActionIndexCache successfulResetActionCode, bool alwaysResetWithAction)
	{
		_successfulResetAction = successfulResetActionCode;
		_alwaysResetWithAction = alwaysResetWithAction;
	}

	public void UpdateSuccessfulResetAction(ActionIndexCache successfulResetActionCode)
	{
		_successfulResetAction = successfulResetActionCode;
	}

	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		ActionIndexCache actionIndexCache = ((isSuccessful || _alwaysResetWithAction) ? _successfulResetAction : ActionIndexCache.act_none);
		float blendInPeriod = ((userAgent.Mission.Mode == MissionMode.Deployment) ? 0f : (-0.2f));
		if (actionIndexCache == ActionIndexCache.act_none)
		{
			userAgent.SetActionChannel(1, in actionIndexCache, ignorePriority: false, (AnimFlags)72uL, 0f, 1f, blendInPeriod);
		}
		userAgent.SetActionChannel(0, in actionIndexCache, ignorePriority: false, (AnimFlags)72uL, 0f, 1f, blendInPeriod);
	}
}
