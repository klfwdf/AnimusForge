namespace TaleWorlds.MountAndBlade;

public struct MissionFormationSpawnData
{
	public int FootTroopCount;

	public int MountedTroopCount;

	public int NumTroops => FootTroopCount + MountedTroopCount;
}
