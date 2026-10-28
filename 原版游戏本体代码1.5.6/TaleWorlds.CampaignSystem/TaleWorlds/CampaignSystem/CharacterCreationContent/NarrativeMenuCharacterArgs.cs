using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent;

public readonly struct NarrativeMenuCharacterArgs(string characterId, int age, string equipmentId, string animationId, string spawnPointEntityId, string leftHandItemId = "", string rightHandItemId = "", MountCreationKey mountCreationKey = null, bool isHuman = true, bool isFemale = false)
{
	public readonly string CharacterId = characterId;

	public readonly int Age = age;

	public readonly string EquipmentId = equipmentId;

	public readonly string AnimationId = animationId;

	public readonly string SpawnPointEntityId = spawnPointEntityId;

	public readonly string LeftHandItemId = leftHandItemId;

	public readonly string RightHandItemId = rightHandItemId;

	public readonly MountCreationKey MountCreationKey = mountCreationKey;

	public readonly bool IsHuman = isHuman;

	public readonly bool IsFemale = isFemale;
}
