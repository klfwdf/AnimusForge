using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public readonly struct NarrativeMenuCharacterArgs
{
	public readonly string CharacterId;

	public readonly int Age;

	public readonly string EquipmentId;

	public readonly string AnimationId;

	public readonly string SpawnPointEntityId;

	public readonly string LeftHandItemId;

	public readonly string RightHandItemId;

	public readonly MountCreationKey MountCreationKey;

	public readonly bool IsHuman;

	public readonly bool IsFemale;

	public NarrativeMenuCharacterArgs(string characterId, int age, string equipmentId, string animationId, string spawnPointEntityId, string leftHandItemId = "", string rightHandItemId = "", MountCreationKey mountCreationKey = null, bool isHuman = true, bool isFemale = false)
	{
		CharacterId = characterId;
		Age = age;
		EquipmentId = equipmentId;
		AnimationId = animationId;
		SpawnPointEntityId = spawnPointEntityId;
		LeftHandItemId = leftHandItemId;
		RightHandItemId = rightHandItemId;
		MountCreationKey = mountCreationKey;
		IsHuman = isHuman;
		IsFemale = isFemale;
	}
}
