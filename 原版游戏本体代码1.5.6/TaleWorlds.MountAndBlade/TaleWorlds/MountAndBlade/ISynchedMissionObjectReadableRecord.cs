namespace TaleWorlds.MountAndBlade;

public interface ISynchedMissionObjectReadableRecord
{
	bool ReadFromNetwork(ref bool bufferReadValid);
}
