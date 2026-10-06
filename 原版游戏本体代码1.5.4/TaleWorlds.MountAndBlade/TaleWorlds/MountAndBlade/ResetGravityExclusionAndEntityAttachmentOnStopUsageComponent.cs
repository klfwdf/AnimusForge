using System;
using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class ResetGravityExclusionAndEntityAttachmentOnStopUsageComponent : UsableMissionObjectComponent
{
	public Action<Agent> OnUseAction;

	public ResetGravityExclusionAndEntityAttachmentOnStopUsageComponent(Action<Agent> onUseAction)
	{
		OnUseAction = onUseAction;
	}

	protected internal override void OnUse(Agent userAgent)
	{
		OnUseAction(userAgent);
	}

	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		userAgent.SetExcludedFromGravity(exclude: false, applyAverageGlobalVelocity: false);
		userAgent.SetForceAttachedEntity(WeakGameEntity.Invalid);
	}
}
