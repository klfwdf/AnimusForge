using System;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x0200020D RID: 525
	public interface ICharacterCreationStateHandler
	{
		// Token: 0x06001FC1 RID: 8129
		void OnCharacterCreationFinalized();

		// Token: 0x06001FC2 RID: 8130
		void OnRefresh();

		// Token: 0x06001FC3 RID: 8131
		void OnStageCreated(CharacterCreationStageBase stage);
	}
}
