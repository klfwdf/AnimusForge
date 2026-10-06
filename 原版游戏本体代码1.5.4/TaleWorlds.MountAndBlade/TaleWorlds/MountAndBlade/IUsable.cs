namespace TaleWorlds.MountAndBlade;

public interface IUsable
{
	void OnUse(Agent userAgent, sbyte agentBoneIndex);

	void OnUseStopped(Agent userAgent, bool isSuccessful, int preferenceIndex);
}
