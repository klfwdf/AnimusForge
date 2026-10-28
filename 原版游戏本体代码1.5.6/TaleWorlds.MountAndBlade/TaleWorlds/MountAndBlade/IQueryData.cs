namespace TaleWorlds.MountAndBlade;

public interface IQueryData
{
	void Expire();

	void Evaluate(float currentTime);

	void SetSyncGroup(IQueryData[] syncGroup);
}
