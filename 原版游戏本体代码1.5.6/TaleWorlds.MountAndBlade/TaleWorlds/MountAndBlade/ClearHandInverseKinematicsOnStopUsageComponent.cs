namespace TaleWorlds.MountAndBlade;

public class ClearHandInverseKinematicsOnStopUsageComponent : UsableMissionObjectComponent
{
	protected internal override void OnUseStopped(Agent userAgent, bool isSuccessful = true)
	{
		userAgent.ClearHandInverseKinematics();
	}
}
