using System;

namespace TaleWorlds.CampaignSystem.CharacterCreationContent
{
	// Token: 0x0200020B RID: 523
	public interface ICharacterCreationContentHandler
	{
		// Token: 0x06001FBC RID: 8124
		void InitializeContent(CharacterCreationManager characterCreationManager);

		// Token: 0x06001FBD RID: 8125
		void AfterInitializeContent(CharacterCreationManager characterCreationManager);

		// Token: 0x06001FBE RID: 8126
		void OnStageCompleted(CharacterCreationStageBase stage);

		// Token: 0x06001FBF RID: 8127
		void OnCharacterCreationFinalize(CharacterCreationManager characterCreationManager);
	}
}
