using TaleWorlds.DotNet;

namespace TaleWorlds.MountAndBlade;

[EngineStruct("Bone_body_part_type", false, null)]
public enum BoneBodyPartType : sbyte
{
	None = -1,
	Head = 0,
	Neck = 1,
	Chest = 2,
	Abdomen = 3,
	ShoulderLeft = 4,
	ShoulderRight = 5,
	ArmLeft = 6,
	ArmRight = 7,
	Legs = 8,
	NumOfBodyPartTypes = 9,
	CriticalBodyPartsBegin = 0,
	CriticalBodyPartsEnd = 6
}
