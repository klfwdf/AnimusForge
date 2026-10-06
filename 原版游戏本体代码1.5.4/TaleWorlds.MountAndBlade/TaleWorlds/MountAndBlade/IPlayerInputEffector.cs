namespace TaleWorlds.MountAndBlade;

public interface IPlayerInputEffector : IMissionBehavior
{
	Agent.EventControlFlag OnCollectPlayerEventControlFlags();
}
