namespace TaleWorlds.MountAndBlade;

public interface IFlagRemoved : IMissionBehavior
{
	void OnFlagsRemoved(int remainingFlagIndex);
}
