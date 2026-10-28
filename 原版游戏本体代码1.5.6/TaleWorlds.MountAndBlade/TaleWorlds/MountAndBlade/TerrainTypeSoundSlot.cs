using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Terrain_type_sound_slot", false, null)]
public enum TerrainTypeSoundSlot : uint
{
	Dismounted,
	ArmyDismounted,
	ArmyMounted,
	Mounted,
	Mounted_slow,
	Caravan,
	Ambient
}
