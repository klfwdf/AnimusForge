using System;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x02000210 RID: 528
	public readonly struct NarrativeMenuCharacterArgs
	{
		// Token: 0x06001FEB RID: 8171 RVA: 0x0008E864 File Offset: 0x0008CA64
		public NarrativeMenuCharacterArgs(string characterId, int age, string equipmentId, string animationId, string spawnPointEntityId, string leftHandItemId = "", string rightHandItemId = "", MountCreationKey mountCreationKey = null, bool isHuman = true, bool isFemale = false)
		{
			this.CharacterId = characterId;
			this.Age = age;
			this.EquipmentId = equipmentId;
			this.AnimationId = animationId;
			this.SpawnPointEntityId = spawnPointEntityId;
			this.LeftHandItemId = leftHandItemId;
			this.RightHandItemId = rightHandItemId;
			this.MountCreationKey = mountCreationKey;
			this.IsHuman = isHuman;
			this.IsFemale = isFemale;
		}

		// Token: 0x04000954 RID: 2388
		public readonly string CharacterId;

		// Token: 0x04000955 RID: 2389
		public readonly int Age;

		// Token: 0x04000956 RID: 2390
		public readonly string EquipmentId;

		// Token: 0x04000957 RID: 2391
		public readonly string AnimationId;

		// Token: 0x04000958 RID: 2392
		public readonly string SpawnPointEntityId;

		// Token: 0x04000959 RID: 2393
		public readonly string LeftHandItemId;

		// Token: 0x0400095A RID: 2394
		public readonly string RightHandItemId;

		// Token: 0x0400095B RID: 2395
		public readonly MountCreationKey MountCreationKey;

		// Token: 0x0400095C RID: 2396
		public readonly bool IsHuman;

		// Token: 0x0400095D RID: 2397
		public readonly bool IsFemale;
	}
}
